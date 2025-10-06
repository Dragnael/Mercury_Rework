using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Nodsoft.Mercury.Data;

namespace Nodsoft.Mercury.Functions.Services.Middlewares;

/// <summary>
/// Represents a middleware for handling access tokens. 
/// </summary>
public class AccessTokenMiddleware : IFunctionsWorkerMiddleware
{
	public const string AccessTokenHeader = "X-AccessToken";
	public const string AccessTokenFormKey = "x-access-token";

	public const string AccessTokenClaim = "AccessToken";
	public const string SourceIdClaim = "SourceId";
	
	public async Task Invoke(FunctionContext funcCtx, FunctionExecutionDelegate next)
	{
		// Only handle HTTP requests ; passthrough for other types of requests.
		if (await funcCtx.GetHttpRequestDataAsync() is not { } req)
		{
			await next(funcCtx);
			return;
		}

		ILogger<AccessTokenMiddleware> logger = funcCtx.InstanceServices.GetRequiredService<ILogger<AccessTokenMiddleware>>();

		try
		{
			// Try first from Header
			Guid? accessToken = null;

			if (req.Headers.TryGetValues(AccessTokenHeader, out IEnumerable<string>? headerValues) && headerValues.FirstOrDefault() is { } headerValue)
			{
				accessToken = Guid.TryParse(headerValue, out Guid parsed) ? parsed : null;
			}
			else if (funcCtx.GetHttpContext() is { Request.Form: { Count: > 0 } form } && form.TryGetValue(AccessTokenFormKey, out StringValues formValue))
			{
				accessToken = formValue.FirstOrDefault()?.Split(' ') is ["Bearer", var token]
					? Guid.TryParse(token, out Guid parsed) ? parsed : null
					: null;
			}

			FormAccessService accessService = funcCtx.InstanceServices.GetRequiredService<FormAccessService>();

			if (accessToken is not null && await accessService.CanAccessSourceAsync(accessToken.Value) is { } sourceId)
			{
				logger.LogDebug("Successfully authenticated access token {AccessToken} for source {SourceId}", accessToken, sourceId);
				funcCtx.Items.Add(SourceIdClaim, sourceId);
				funcCtx.Items.Add(AccessTokenClaim, accessToken);
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error authenticating access token");
		}

		await next(funcCtx);
	}

	private static async Task<Dictionary<string, string>> ParseFormAsync(HttpRequestData request)
	{
		Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

		if (!request.Headers.TryGetValues("Content-Type", out IEnumerable<string>? ctValues))
		{
			return result;
		}

		string? contentTypeHeader = ctValues.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(contentTypeHeader))
		{
			return result;
		}

		string mediaType = contentTypeHeader.Split(';')[0].Trim().ToLowerInvariant();

		// Read entire body into memory (buffer) so we can parse safely.
		// If the underlying stream is seekable we reset position afterwards.
		string body;
		using (StreamReader sr = new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
		{
			body = await sr.ReadToEndAsync(request.FunctionContext.CancellationToken);
		}

		if (request.Body.CanSeek)
		{
			request.Body.Position = 0;
		}

		if (mediaType is "application/x-www-form-urlencoded")
		{
			Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(body);
			foreach (KeyValuePair<string, StringValues> kvp in parsed.Where(kvp => kvp.Value.Count > 0))
			{
				result[kvp.Key] = kvp.Value[0];
			}

			return result;
		}

		if (mediaType.StartsWith("multipart/"))
		{
			MediaTypeHeaderValue media = MediaTypeHeaderValue.Parse(contentTypeHeader);
			string? boundary = HeaderUtilities.RemoveQuotes(media.Boundary).Value;
			
			if (string.IsNullOrEmpty(boundary))
			{
				return result;
			}

			// Parse multipart from the buffered body
			using MemoryStream bufferStream = new(Encoding.UTF8.GetBytes(body));
			MultipartReader reader = new(boundary, bufferStream);
			while (await reader.ReadNextSectionAsync(request.FunctionContext.CancellationToken) is { } section)
			{
				if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition) 
					|| !contentDisposition.IsFormDisposition())
				{
					continue;
				}

				string name = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value ?? "";
				using StreamReader sectionReader = new(section.Body, Encoding.UTF8);
				string value = await sectionReader.ReadToEndAsync(request.FunctionContext.CancellationToken);
				result[name] = value;
			}
		}

		return result;
	}
}