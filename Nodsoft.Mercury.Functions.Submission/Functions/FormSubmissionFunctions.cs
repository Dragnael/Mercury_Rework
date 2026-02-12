using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Functions.Submission.Extensions;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Submission.Functions;

/// <summary>
/// Represents functions for form submissions.
/// Secure, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormSubmissionFunctions
{
    public const string FormTemplateFormKey = "x-template-id";

    private readonly FormSubmissionService _service;
    private readonly ILogger<FormSubmissionFunctions> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormSubmissionFunctions"/> class.
    /// </summary>
    public FormSubmissionFunctions(
        FormSubmissionService service,
        ILogger<FormSubmissionFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form submission by its ID.
    /// </summary>
    [Function("GetSubmission")]
    public async Task<ActionResult> GetSubmissionAsync(
        FunctionContext ctx,
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "submission/{id:guid}")]
        HttpRequest req,
        Guid id)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        if (ctx.GetAccessTokenId() is not { } accessTokenId ||
            ctx.GetSourceId() is not { } sourceId)
        {
            return new UnauthorizedResult();
        }

        var submission = await _service.GetByIdAsync(id, ctx.CancellationToken);

        if (submission is null)
            return new NotFoundResult();

        if (submission.FormSourceId != sourceId ||
            submission.AccessTokenId != accessTokenId)
        {
            return new ForbidResult();
        }

        return new OkObjectResult(submission.Adapt<FormSubmissionDto>());
    }

    /// <summary>
    /// Ingests a new form submission.
    /// Supports JSON and multipart/form-data.
    /// </summary>
    [Function("IngestSubmission")]
    public async Task<FormSubmissionOutput> IngestSubmissionAsync(
        FunctionContext ctx,
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "submission")]
        HttpRequest req)
    {
        if (!req.IsHttps)
            return new(new StatusCodeResult(StatusCodes.Status403Forbidden));

        if (ctx.GetAccessTokenId() is not { } accessTokenId ||
            ctx.GetSourceId() is not { } sourceId)
        {
            return new(new UnauthorizedResult());
        }

        FormSubmissionDto? submissionDto = null;

        try
        {
            if (req.HasFormContentType)
            {
                var form = await req.ReadFormAsync(ctx.CancellationToken);

                var fields = new Dictionary<string, object>();
                Guid formTemplateId = Guid.Empty;

                foreach ((string key, StringValues value) in form)
                {
                    switch (key)
                    {
                        case AccessTokenMiddleware.AccessTokenFormKey:
                            break;

                        case FormTemplateFormKey:
                            if (!Guid.TryParse(value, out formTemplateId))
                            {
                                return new(new BadRequestObjectResult("Invalid template ID."));
                            }
                            break;

                        default:
                            fields[key] = value.Count > 1
                                ? value.ToArray()
                                : value.FirstOrDefault()!;
                            break;
                    }
                }

                if (formTemplateId == Guid.Empty)
                    return new(new BadRequestObjectResult("Template ID is required."));

                if (fields.Count == 0)
                    return new(new BadRequestObjectResult("Submission data cannot be empty."));

                submissionDto = new()
                {
                    AccessTokenId = accessTokenId,
                    FormTemplateId = formTemplateId,
                    Data = fields,
                    Created = DateTimeOffset.UtcNow,
                    SubmittedBy = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                };
            }
            else
            {
                var body = await req.ReadFromJsonAsync<FormSubmissionDto>(ctx.CancellationToken);

                if (body is null ||
                    body.FormTemplateId == Guid.Empty ||
                    body.Data is null ||
                    body.Data.Count == 0)
                {
                    return new(new BadRequestObjectResult("Invalid submission payload."));
                }

                submissionDto = new()
                {
                    AccessTokenId = accessTokenId,
                    FormTemplateId = body.FormTemplateId,
                    Data = body.Data,
                    Created = DateTimeOffset.UtcNow,
                    SubmittedBy = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                };
            }

            var entity = submissionDto.Adapt<FormSubmission>();
            var created = await _service.AddAsync(entity, ctx.CancellationToken);

            return new(
                new CreatedResult($"/api/submission/{created.Id}",
                    created.Adapt<FormSubmissionDto>()),
                created.Adapt<FormSubmissionNotificationDto>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error ingesting submission for SourceId {SourceId}",
                sourceId);

            return new(new StatusCodeResult(StatusCodes.Status500InternalServerError));
        }
    }

    [UsedImplicitly]
    public sealed record FormSubmissionOutput(
        [property: HttpResult]
        IActionResult HttpResult,

        [property: ServiceBusOutput("submissions",
            EntityType = ServiceBusEntityType.Queue,
            Connection = "submissions-queue")]
        FormSubmissionNotificationDto? MqNotification = null
    );
}
