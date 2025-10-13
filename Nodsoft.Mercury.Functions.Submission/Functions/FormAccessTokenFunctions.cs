using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Functions.Submission.Services;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Submission.Functions;

/// <summary>
/// Represents functions for form access tokens. 
/// </summary>
public sealed class FormAccessTokenFunctions
{
	private readonly FormAccessService _service;
	private readonly ILogger<FormAccessTokenFunctions> _logger;

	public FormAccessTokenFunctions(FormAccessService service, ILogger<FormAccessTokenFunctions> logger)
	{
		_service = service;
		_logger = logger;
	}

	/// <summary>
	/// Gets a form access token by its ID.
	/// </summary>
	[Function("GetAccessToken")]
	public async Task<IActionResult> GetAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "token/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id)
	{
		if (await _service.GetByIdAsync(id, req.HttpContext.RequestAborted) is not { } token)
		{
			return new NotFoundResult();
		}

		return new OkObjectResult(token.Adapt<FormSourceAccessTokenDto>());
	}

	/// <summary>
	/// Creates a new form access token.
	/// </summary>
	[Function("CreateAccessToken")]
	public async Task<IActionResult> CreateAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = "token")] HttpRequest req
	) {
		/* Two possible inputs : 
		 * - Body w/ full DTO
		 * - Query string w/ source ID and expiration date (optional)
		 */

		FormSourceAccessTokenDto? tokenDto = null;
		
		if (Guid.TryParse(req.Query["sourceId"], out Guid sourceId))
		{
			tokenDto = new()
			{
				Id = Guid.CreateVersion7(),
				FormSourceId = sourceId,
				Expires = DateTimeOffset.TryParse(req.Query["expires"], out DateTimeOffset expires) ? expires : null,
				Revoked = false,
				Created = DateTimeOffset.UtcNow
			};
		}
		else if (await req.ReadFromJsonAsync<FormSourceAccessTokenDto>() is { } dto)
		{
			tokenDto = dto;
		}
		else
		{
			return new BadRequestResult();
		}

		try
		{
			FormAccessToken created = await _service.CreateAsync(tokenDto.FormSourceId, tokenDto.Expires, req.HttpContext.RequestAborted);
			return new CreatedResult($"/api/token/{created.Id}", created.Adapt<FormSourceAccessTokenDto>());
		}
		catch (ArgumentException ex)
		{
			// Invalid source ID
			return new BadRequestObjectResult(ex.Message);
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating access token");
			throw;
		}
	}

	/// <summary>
	/// Revokes (deletes) a form access token by its ID.
	/// </summary>
	[Function("DeleteAccessToken")]
	public async Task<IActionResult> DeleteAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "delete", Route = "token/{id:guid}")] HttpRequest req,
		[FromRoute] Guid id)
	{
		try
		{
			bool revoked = await _service.RevokeAsync(id, req.HttpContext.RequestAborted);
			if (!revoked)
			{
				return new NotFoundResult();
			}
			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error revoking access token");
			return new StatusCodeResult(500);
		}
	}
}