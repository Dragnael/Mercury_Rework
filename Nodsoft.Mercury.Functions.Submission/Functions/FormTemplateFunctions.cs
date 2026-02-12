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
/// Represents functions for form templates.
/// Secure, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormTemplateFunctions
{
    private readonly FormTemplateService _service;
    private readonly ILogger<FormTemplateFunctions> _logger;

    public FormTemplateFunctions(
        FormTemplateService service,
        ILogger<FormTemplateFunctions> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form template by its ID.
    /// </summary>
    [Function("GetTemplate")]
    public async Task<IActionResult> GetTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "template/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var template = await _service.GetByIdAsync(id, cancellationToken);

        if (template is null)
            return new NotFoundResult();

        return new OkObjectResult(template.Adapt<FormTemplateDto>());
    }

    /// <summary>
    /// Gets all form templates.
    /// </summary>
    [Function("GetAllTemplates")]
    public async Task<IActionResult> GetAllTemplatesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "template")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var templates = await _service.GetAllAsync(cancellationToken);

        return new OkObjectResult(
            templates.Select(t => t.Adapt<FormTemplateDto>()));
    }

    /// <summary>
    /// Creates a new form template.
    /// </summary>
    [Function("CreateTemplate")]
    public async Task<IActionResult> CreateTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "template")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        try
        {
            var templateDto =
                await req.ReadFromJsonAsync<FormTemplateDto>(cancellationToken);

            if (templateDto is null ||
                string.IsNullOrWhiteSpace(templateDto.Name) ||
                templateDto.SourceId == Guid.Empty)
            {
                return new BadRequestObjectResult("Invalid template payload.");
            }

            var entity = templateDto.Adapt<FormTemplate>();

            var created = await _service.CreateAsync(entity, cancellationToken);

            return new CreatedResult(
                $"/api/template/{created.Id}",
                created.Adapt<FormTemplateDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form template");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing form template.
    /// </summary>
    [Function("UpdateTemplate")]
    public async Task<IActionResult> UpdateTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "template/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        try
        {
            var templateDto =
                await req.ReadFromJsonAsync<FormTemplateDto>(cancellationToken);

            if (templateDto is null)
                return new BadRequestResult();

            var existing = await _service.GetByIdAsync(id, cancellationToken);

            if (existing is null)
                return new NotFoundResult();

            // Update selective fields safely
            existing.Name = templateDto.Name;
            existing.SourceId = templateDto.SourceId;
            existing.AllowExtraFields = templateDto.AllowExtraFields;

            existing.Fields = templateDto.Fields?
                .Select(f => new FormTemplateField(
                    f.Path,
                    f.Regex,
                    f.Required))
                .ToList() ?? new List<FormTemplateField>();

            var updated = await _service.UpdateAsync(existing, cancellationToken);

            return new OkObjectResult(updated.Adapt<FormTemplateDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form template {TemplateId}", id);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes a form template by its ID.
    /// </summary>
    [Function("DeleteTemplate")]
    public async Task<IActionResult> DeleteTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "template/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!req.IsHttps)
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return new NotFoundResult();

            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form template {TemplateId}", id);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
