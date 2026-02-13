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

namespace Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

/// <summary>
/// Represents a middleware for handling access tokens.
/// Validates access token from header or form-data.
/// Enforces HTTPS usage.
/// Compatible with Azure reverse proxy scenarios.
/// </summary>
public sealed class AccessTokenMiddleware : IFunctionsWorkerMiddleware
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

        ILogger<AccessTokenMiddleware> logger =
            funcCtx.InstanceServices.GetRequiredService<ILogger<AccessTokenMiddleware>>();

        try
        {
            // Enforce HTTPS (direct or forwarded from Azure proxy)
            if (!IsHttpsRequest(funcCtx))
            {
                logger.LogWarning("Rejected non-HTTPS request.");
                await next(funcCtx);
                return;
            }

            // Try first from Header
            Guid? accessToken = await ResolveAccessTokenAsync(funcCtx, req);

            if (accessToken is null)
            {
                await next(funcCtx);
                return;
            }

            FormAccessService accessService =
                funcCtx.InstanceServices.GetRequiredService<FormAccessService>();

            if (await accessService.CanAccessSourceAsync(accessToken.Value) is { } sourceId)
            {
                logger.LogDebug(
                    "Successfully authenticated access token {AccessToken} for source {SourceId}",
                    accessToken,
                    sourceId);

                funcCtx.Items[SourceIdClaim] = sourceId;
                funcCtx.Items[AccessTokenClaim] = accessToken;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Access token validation cancelled.");
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error authenticating access token.");
        }

        await next(funcCtx);
    }

    /// <summary>
    /// Ensures the request is HTTPS.
    /// Supports Azure reverse proxy (X-Forwarded-Proto).
    /// </summary>
    private static bool IsHttpsRequest(FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
            return true; // Non HTTP scenario

        if (httpContext.Request.IsHttps)
            return true;

        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto)
            && string.Equals(proto, "https", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves access token from header or form-data (urlencoded or multipart).
    /// </summary>
    private static async Task<Guid?> ResolveAccessTokenAsync(
        FunctionContext context,
        HttpRequestData request)
    {
        // 1️⃣ Header first
        if (request.Headers.TryGetValues(AccessTokenHeader, out IEnumerable<string>? headerValues)
            && headerValues.FirstOrDefault() is { } headerValue
            && Guid.TryParse(headerValue, out Guid parsedHeader))
        {
            return parsedHeader;
        }

        // 2️⃣ Fallback to Form
        Dictionary<string, string> form =
            await ParseFormAsync(request, context.CancellationToken);

        if (form.TryGetValue(AccessTokenFormKey, out string? formValue)
            && Guid.TryParse(formValue, out Guid parsedForm))
        {
            return parsedForm;
        }

        return null;
    }

    /// <summary>
    /// Parses form content safely.
    /// Supports:
    /// - application/x-www-form-urlencoded
    /// - multipart/form-data (streaming)
    /// </summary>
    private static async Task<Dictionary<string, string>> ParseFormAsync(
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> result =
            new(StringComparer.OrdinalIgnoreCase);

        if (!request.Headers.TryGetValues("Content-Type", out IEnumerable<string>? ctValues))
        {
            return result;
        }

        string? contentTypeHeader = ctValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(contentTypeHeader))
        {
            return result;
        }

        string mediaType = contentTypeHeader.Split(';')[0]
            .Trim()
            .ToLowerInvariant();

        // application/x-www-form-urlencoded
        if (mediaType is "application/x-www-form-urlencoded")
        {
            using StreamReader sr =
                new(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            string body = await sr.ReadToEndAsync(cancellationToken);

            if (request.Body.CanSeek)
                request.Body.Position = 0;

            Dictionary<string, StringValues> parsed =
                QueryHelpers.ParseQuery(body);

            foreach (KeyValuePair<string, StringValues> kvp in parsed)
            {
                if (kvp.Value.Count > 0)
                    result[kvp.Key] = kvp.Value[0];
            }

            return result;
        }

        // multipart/form-data (streaming, no full buffering)
        if (mediaType.StartsWith("multipart/"))
        {
            MediaTypeHeaderValue media =
                MediaTypeHeaderValue.Parse(contentTypeHeader);

            string? boundary =
                HeaderUtilities.RemoveQuotes(media.Boundary).Value;

            if (string.IsNullOrEmpty(boundary))
                return result;

            MultipartReader reader =
                new(boundary, request.Body);

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) is not null)
            {
                if (!ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition,
                        out ContentDispositionHeaderValue? contentDisposition)
                    || !contentDisposition.IsFormDisposition())
                {
                    continue;
                }

                string name =
                    HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value ?? "";

                using StreamReader sectionReader =
                    new(section.Body, Encoding.UTF8);

                string value =
                    await sectionReader.ReadToEndAsync(cancellationToken);

                result[name] = value;
            }
        }

        return result;
    }
}
