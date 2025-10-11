using System.Web.Http;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Submission.Functions;

/// <summary>
/// Represents functions for form templates.
/// </summary>
public sealed class FormTemplateFunctions
{
	private readonly FormTemplateService _service;
	private readonly ILogger<FormTemplateFunctions> _logger;

	public FormTemplateFunctions(FormTemplateService service, ILogger<FormTemplateFunctions> logger)
	{
		_service = service;
		_logger = logger;
	}

	/// <summary>
	/// Gets a form template by its ID.
	/// </summary>
	[Function("GetTemplate")]
	public async Task<IActionResult> GetTemplateAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "template/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id)
	{
		if (await _service.GetByIdAsync(id) is not { } template)
		{
			return new NotFoundResult();
		}

		return new OkObjectResult(template.Adapt<FormTemplateDto>());
	}

	/// <summary>
	/// Gets all form templates.
	/// </summary>
	[Function("GetAllTemplates")]
	public async Task<IActionResult> GetAllTemplatesAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "template")] HttpRequest req)
	{
		List<FormTemplate> templates = await _service.GetAllAsync();
		return new OkObjectResult(templates.Select(t => t.Adapt<FormTemplateDto>()));
	}

	/// <summary>
	/// Creates a new form template.
	/// </summary>
	[Function("CreateTemplate")]
	public async Task<IActionResult> CreateTemplateAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "template")] HttpRequest req)
	{
		if (await req.ReadFromJsonAsync<FormTemplateDto>() is not { } templateDto)
		{
			return new BadRequestResult();
		}

		try
		{
			FormTemplate template = templateDto.Adapt<FormTemplate>();
			FormTemplate created = await _service.CreateAsync(template);
			return new CreatedResult($"/api/template/{created.Id}", created.Adapt<FormTemplateDto>());
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating form template");
			return new InternalServerErrorResult();
		}
	}

	/// <summary>
	/// Updates an existing form template.
	/// </summary>
	[Function("UpdateTemplate")]
	public async Task<IActionResult> UpdateTemplateAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "template/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id,
		[FromBody] FormTemplateDto? templateDto)
	{
		if (templateDto is null)
		{
			return new BadRequestResult();
		}

		try
		{
			if (await _service.GetByIdAsync(id) is not { } existing)
			{
				return new NotFoundResult();
			}

			// Update selective fields
			existing.Name = templateDto.Name;
			existing.SourceId = templateDto.SourceId;
			existing.AllowExtraFields = templateDto.AllowExtraFields;
			existing.Fields = templateDto.Fields.Select(f => new FormTemplateField(f.Path, f.Regex, f.Required)).ToList();

			FormTemplate updated = await _service.UpdateAsync(existing);
			return new OkObjectResult(updated.Adapt<FormTemplateDto>());
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating form template");
			return new InternalServerErrorResult();
		}
	}

	/// <summary>
	/// Deletes a form template by its ID.
	/// </summary>
	[Function("DeleteTemplate")]
	public async Task<IActionResult> DeleteTemplateAsync([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "template/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id)
	{
		try
		{
			bool deleted = await _service.DeleteAsync(id);
			if (!deleted)
			{
				return new NotFoundResult();
			}
			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting form template");
			return new StatusCodeResult(500);
		}
	}
}