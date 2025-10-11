using JetBrains.Annotations;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Functions.Submission.Extensions;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;
using Nodsoft.Mercury.Models;
using Throw;

namespace Nodsoft.Mercury.Functions.Submission.Functions;

/// <summary>
/// Represents functions for form submissions. 
/// </summary>
public sealed class FormSubmissionFunctions
{
	public const string FormTemplateFormKey = "x-template-id";
	
	private readonly FormSubmissionService _service;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormSubmissionFunctions"/> class.
	/// </summary>
	public FormSubmissionFunctions(FormSubmissionService service, FormTemplateService templateService)
	{
		_service = service;
	}

	/// <summary>
	/// Gets a form submission by its ID.
	/// </summary>
	/// <param name="ctx">The function context.</param>
	/// <param name="req">The HTTP request.</param>
	/// <param name="id">The ID of the form submission.</param>
	/// <returns>The form submission with the specified ID if found.</returns>
	[Function("GetSubmission")]
	public async Task<ActionResult> GetSubmissionAsync(
		FunctionContext ctx, 
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "submission/{id:guid}")] HttpRequest req, 
		[FromRoute] Guid id
	) {
		if (ctx.GetAccessTokenId() is not { } accessTokenId || ctx.GetSourceId() is not { } sourceId)
		{
			return new UnauthorizedResult();
		}
		
		FormSubmission? submission = await _service.GetByIdAsync(id, ctx.CancellationToken);

		if (submission is null)
		{
			return new NotFoundResult();
		}
		
		if (submission.FormSourceId != sourceId || submission.AccessTokenId != accessTokenId)
		{
			return new ForbidResult();
		}

		return new OkObjectResult(submission.Adapt<FormSubmissionDto>());
	}

	/// <summary>
	/// Gets all form submissions for a given form source.
	/// </summary>
	/// <param name="ctx">The function context.</param>
	/// <param name="req">The HTTP request.</param>
	/// <returns>All form submissions.</returns>
	// [Function("GetSubmissions")]
	public async Task<ActionResult<IAsyncEnumerable<FormSubmissionDto>>> GetSubmissionsAsync(
		FunctionContext ctx,
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "submissions")]
		HttpRequest req
	) {
		if (ctx.GetAccessTokenId() is not { } accessTokenId || ctx.GetSourceId() is not { } sourceId)
		{
			return new UnauthorizedResult();
		}
		
		IQueryable<FormSubmission> submissions = _service.GetAllForSource(sourceId);

		return new OkObjectResult(submissions
			.ProjectToType<FormSubmissionDto>()
			.AsAsyncEnumerable()
		);
	}
	
	///	<summary>
	/// Ingests a new form submission.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <returns>The created form submission.</returns>
	[Function("IngestSubmission")]
	public async Task<FormSubmissionOutput> IngestSubmissionAsync(
		FunctionContext ctx,
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = "submission")] HttpRequest req
	) {
		if (ctx.GetAccessTokenId() is not { } accessTokenId || ctx.GetSourceId() is not { } sourceId)
		{
			return new(new UnauthorizedResult());
		}
		
		/*
		 * Two possible inputs :
		 * - JSON Body w/ full or partial DTO
		 * - Form data w/ fields
		 */
		
		FormSubmissionDto? submissionDto = null;
		
		if (req.HasFormContentType)
		{
			Dictionary<string, object> fields = [];
			Guid formTemplateId = Guid.Empty;
			
			// Extract all fields from DTO, save for AccessToken (as defined by auth middleware).
			foreach ((string key, StringValues value) in await req.ReadFormAsync(ctx.CancellationToken))
			{
				switch (key)
				{
					case AccessTokenMiddleware.AccessTokenFormKey: 
						break;
					
					case FormTemplateFormKey: 
						formTemplateId = Guid.Parse(value); 
						break;
					
					default:
						fields.Add(key, value.Count > 1 ? value : value.FirstOrDefault()!);
						break;
				}
			}

			if (formTemplateId == Guid.Empty)
			{
				return new(new BadRequestResult());
			}
			
			submissionDto = new()
			{
				
				AccessTokenId = accessTokenId,
				Data = fields,
				FormTemplateId = formTemplateId,
				Created = DateTimeOffset.UtcNow,
				SubmittedBy = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
			};
		}
		else if (await req.ReadFromJsonAsync<FormSubmissionDto>(ctx.CancellationToken) is { } body)
		{
			submissionDto = new()
			{
				AccessTokenId = accessTokenId,
				Data = body.Data,
				FormTemplateId = body.FormTemplateId,
				Created = DateTimeOffset.UtcNow,
				SubmittedBy = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
			};
		}
		
		if (submissionDto is null)
		{
			return new(new BadRequestResult());
		}
		
		FormSubmission created = await _service.AddAsync(submissionDto.Adapt<FormSubmission>(), ctx.CancellationToken);
		return new(
			new CreatedResult($"/api/submission/{created.Id}", created.Adapt<FormSubmissionDto>()),
			created.Adapt<FormSubmissionNotificationDto>()
		);
	}
	
	[UsedImplicitly]
	public sealed record FormSubmissionOutput(
		[property: HttpResult] 
		IActionResult HttpResult,
		
		[property: ServiceBusOutput("submissions", EntityType = ServiceBusEntityType.Queue, Connection = "submissions-queue")]
		FormSubmissionNotificationDto? MqNotification = null
	);
}