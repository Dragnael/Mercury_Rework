using System.ComponentModel.DataAnnotations;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Submission.Functions;

/// <summary>
/// Represents HTTP functions used to manage form sources.
/// Secure, Azure-ready, ASP.NET Core compatible and Aspire friendly.
/// </summary>
public sealed class FormSourceFunctions
{
    private readonly FormSourceService _service;
    private readonly ILogger<FormSourceFunctions> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormSourceFunctions"/> class.
    /// </summary>
    public FormSourceFunctions(
        FormSourceService service,
        ILogger<FormSourceFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form source by its ID.
    /// </summary>
    [Function("GetSource")]
    public async Task<IActionResult> GetSourceAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "source/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var source = await _service.GetFormSourceByIdAsync(id, cancellationToken);

        if (source is null)
            return new NotFoundResult();

        return new OkObjectResult(source.Adapt<FormSourceDto>());
    }

    /// <summary>
    /// Gets all form sources.
    /// </summary>
    [Function("GetAllSources")]
    public async Task<IActionResult> GetAllSourcesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "source")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var sources = await _service.GetAllFormSourcesAsync(cancellationToken);

        return new OkObjectResult(
            sources.Select(s => s.Adapt<FormSourceDto>()));
    }

    /// <summary>
    /// Creates a new form source.
    /// Supports JSON and multipart/form-data.
    /// </summary>
    [Function("CreateSource")]
    public async Task<IActionResult> CreateSourceAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "source")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        FormSourceDto? sourceDto;

        if (req.HasFormContentType)
        {
            var form = await req.ReadFormAsync(cancellationToken);

            sourceDto = new FormSourceDto
            {
                Name = form["Name"],
                Url = form["Url"],
                ContactEmail = form["ContactEmail"]
            };
        }
        else
        {
            sourceDto = await req.ReadFromJsonAsync<FormSourceDto>(cancellationToken);
        }

        if (sourceDto is null)
            return new BadRequestObjectResult("Invalid payload.");

        // Basic validation
        if (string.IsNullOrWhiteSpace(sourceDto.Name) ||
            string.IsNullOrWhiteSpace(sourceDto.Url))
        {
            return new BadRequestObjectResult("Name and Url are required.");
        }

        if (!new EmailAddressAttribute().IsValid(sourceDto.ContactEmail))
        {
            return new BadRequestObjectResult("Invalid email format.");
        }

        try
        {
            var entity = sourceDto.Adapt<FormSource>();
            var created = await _service.AddFormSourceAsync(entity, cancellationToken);

            return new CreatedResult(
                $"/api/source/{created.Id}",
                created.Adapt<FormSourceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating form source");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing form source.
    /// </summary>
    [Function("UpdateSource")]
    public async Task<IActionResult> UpdateSourceAsync(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "source/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var sourceDto = await req.ReadFromJsonAsync<FormSourceDto>(cancellationToken);

        if (sourceDto is null)
            return new BadRequestResult();

        var existing = await _service.GetFormSourceByIdAsync(id, cancellationToken);

        if (existing is null)
            return new NotFoundResult();

        existing.Name = sourceDto.Name;
        existing.Url = sourceDto.Url;
        existing.ContactEmail = sourceDto.ContactEmail;

        try
        {
            var updated = await _service.UpdateFormSourceAsync(existing, cancellationToken);

            return new OkObjectResult(updated.Adapt<FormSourceDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form source {SourceId}", id);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes a form source by its ID.
    /// </summary>
    [Function("DeleteSource")]
    public async Task<IActionResult> DeleteSourceAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "source/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        try
        {
            var deleted = await _service.DeleteFormSourceAsync(id, cancellationToken);

            if (!deleted)
                return new NotFoundResult();

            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form source {SourceId}", id);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
}