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
/// Represents functions for form sources.
/// </summary>
public sealed class FormSourceFunctions
{
	private readonly FormSourceService _service;
	private readonly ILogger<FormSourceFunctions> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormSourceFunctions"/> class.
	/// </summary>
	public FormSourceFunctions(FormSourceService service, ILogger<FormSourceFunctions> logger)
	{
		_service = service;
		_logger = logger;
	}
	
	/// <summary>
	/// Gets a form source by its ID.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <param name="id">The ID of the form source.</param>
	/// <returns>The form source with the specified ID if found.</returns>
	[Function("GetSource")]
	public async Task<IActionResult> GetSourceAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "source/{id:guid}")] HttpRequest req, 
		[FromRoute] Guid id
	) {
		if (await _service.GetFormSourceByIdAsync(id) is not { } source)
		{
			return new NotFoundResult();
		}
		
		return new OkObjectResult(source.Adapt<FormSourceDto>());
	}

	/// <summary>
	/// Gets all form sources.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <returns>All form sources.</returns>
	[Function("GetAllSources")]
	public async Task<IActionResult> GetAllSourcesAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "source")] HttpRequest req)
	{
		List<FormSource> sources = await _service.GetAllFormSourcesAsync();
		return new OkObjectResult(sources.Select(s => s.Adapt<FormSourceDto>()));
	}

	/// <summary>
	/// Creates a new form source.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <param name="sourceDto">The form source data to create.</param>
	/// <returns>The created form source.</returns>
	[Function("CreateSource")]
	public async Task<IActionResult> CreateSourceAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "source")] HttpRequest req) 
	{
		if (await req.ReadFromJsonAsync<FormSourceDto>() is not { } sourceDto)
		{
			return new BadRequestResult();
		}
		
		try
		{
			FormSource source = sourceDto.Adapt<FormSource>();
			FormSource createdSource = await _service.AddFormSourceAsync(source);

			return new CreatedResult($"/api/source/{createdSource.Id}", createdSource.Adapt<FormSourceDto>());
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format");
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error creating form source");
			return new InternalServerErrorResult();
		}
	}

	/// <summary>
	/// Updates an existing form source.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <param name="id">The ID of the form source to update.</param>
	/// <param name="sourceDto">The updated form source data.</param>
	/// <returns>The updated form source.</returns>
	[Function("UpdateSource")]
	public async Task<IActionResult> UpdateSourceAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "source/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id,
		[FromBody] FormSourceDto? sourceDto
	) {
		if (sourceDto is null)
		{
			return new BadRequestResult();
		}
		
		try
		{
			if (await _service.GetFormSourceByIdAsync(id) is not { } existingSource)
			{
				return new NotFoundResult();
			}

			// Update the existing source with new values
			existingSource.Name = sourceDto.Name;
			existingSource.Url = sourceDto.Url;
			existingSource.ContactEmail = sourceDto.ContactEmail;

			FormSource updatedSource = await _service.UpdateFormSourceAsync(existingSource);

			return new OkObjectResult(updatedSource.Adapt<FormSourceDto>());
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format");
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error updating form source");
			return new InternalServerErrorResult();
		}
	}

	/// <summary>
	/// Deletes a form source by its ID.
	/// </summary>
	/// <param name="req">The HTTP request.</param>
	/// <param name="id">The ID of the form source to delete.</param>
	/// <returns>No content if successful, not found if the source doesn't exist.</returns>
	[Function("DeleteSource")]
	public async Task<IActionResult> DeleteSourceAsync([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "source/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id
	) {
		try
		{
			bool deleted = await _service.DeleteFormSourceAsync(id);

			if (!deleted)
			{
				return new NotFoundResult();
			}

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting form source");
			return new StatusCodeResult(500);
		}
	}
}