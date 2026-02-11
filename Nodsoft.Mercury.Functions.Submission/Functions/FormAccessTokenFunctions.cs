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
/// Represents functions for form access tokens.
/// </summary>
public sealed class FormAccessTokenFunctions
{
	private readonly FormAccessService _service;
	private readonly ILogger<FormAccessTokenFunctions> _logger;

	public FormAccessTokenFunctions(
		FormAccessService service,
		ILogger<FormAccessTokenFunctions> logger)
	{
		_service = service;
		_logger = logger;
	}

	/// <summary>
	/// Gets a form access token by its ID.
	/// </summary>
	[Function("GetAccessToken")]
	public async Task<IActionResult> GetAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "token/{id:guid}")]
		HttpRequest req,
		[FromRoute] Guid id)
	{
		var ct = req.HttpContext.RequestAborted;

		var token = await _service.GetByIdAsync(id, ct);

		if (token is null)
			return new NotFoundResult();

		return new OkObjectResult(token.Adapt<FormSourceAccessTokenDto>());
	}

	/// <summary>
	/// Creates a new form access token.
	/// </summary>
	[Function("CreateAccessToken")]
	public async Task<IActionResult> CreateAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = "token")]
		HttpRequest req)
	{
		var ct = req.HttpContext.RequestAborted;

		var request = await ParseCreateRequestAsync(req);

		if (request is null)
			return new BadRequestObjectResult("Invalid request payload.");

		try
		{
			var created = await _service.CreateAsync(
				request.FormSourceId,
				request.Expires,
				ct);

			return new CreatedResult(
				$"/api/token/{created.Id}",
				created.Adapt<FormSourceAccessTokenDto>());
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex,
				"Invalid source ID {SourceId} while creating access token.",
				request.FormSourceId);

			return new BadRequestObjectResult(ex.Message);
		}
		catch (JsonException)
		{
			return new BadRequestObjectResult("Invalid JSON format.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"Unexpected error while creating access token for source {SourceId}.",
				request.FormSourceId);

			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Revokes a form access token by its ID.
	/// </summary>
	[Function("DeleteAccessToken")]
	public async Task<IActionResult> DeleteAccessTokenAsync(
		[HttpTrigger(AuthorizationLevel.Function, "delete", Route = "token/{id:guid}")]
		HttpRequest req,
		[FromRoute] Guid id)
	{
		var ct = req.HttpContext.RequestAborted;

		try
		{
			var revoked = await _service.RevokeAsync(id, ct);

			if (!revoked)
				return new NotFoundResult();

			return new NoContentResult();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"Unexpected error while revoking access token {TokenId}.",
				id);

			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}
	}

	/// <summary>
	/// Parses the create request from either query string or JSON body.
	/// </summary>
	private static async Task<CreateFormAccessTokenRequest?> ParseCreateRequestAsync(HttpRequest req)
	{
		// Query string mode
		if (Guid.TryParse(req.Query["sourceId"], out var sourceId))
		{
			DateTimeOffset? expires = null;

			if (DateTimeOffset.TryParse(req.Query["expires"], out var parsedExpires))
				expires = parsedExpires;

			return new CreateFormAccessTokenRequest
			{
				FormSourceId = sourceId,
				Expires = expires
			};
		}

		// Body mode
		return await req.ReadFromJsonAsync<CreateFormAccessTokenRequest>();
	}
}
