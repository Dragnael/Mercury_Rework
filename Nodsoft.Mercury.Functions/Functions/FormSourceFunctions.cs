using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nodsoft.Mercury.Data.Data;
using Nodsoft.Mercury.Functions.Services;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Functions;

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
}