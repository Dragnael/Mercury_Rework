using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

namespace Nodsoft.Mercury.Functions.Submission.Extensions;

/// <summary>
/// Provides extensions for working with HTTP requests made to Azure Functions.
/// </summary>
public static class RequestExtensions
{
    /// <summary>
    /// Gets the ID of the access token associated with the request.
    /// </summary>
    /// <param name="ctx">The function context.</param>
    /// <returns>The ID of the access token, or <see langword="null"/> if not found.</returns>
    public static Guid? GetAccessTokenId(this FunctionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (ctx.Items.TryGetValue(AccessTokenMiddleware.AccessTokenClaim, out object? value))
        {
            if (value is Guid guid)
                return guid;

            if (value is string s && Guid.TryParse(s, out Guid parsed))
                return parsed;
        }

        return null;
    }

    /// <summary>
    /// Gets the ID of the form source associated with the request.
    /// </summary>
    /// <param name="ctx">The function context.</param>
    /// <returns>The ID of the form source, or <see langword="null"/> if not found.</returns>
    public static Guid? GetSourceId(this FunctionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (ctx.Items.TryGetValue(AccessTokenMiddleware.SourceIdClaim, out object? value))
        {
            if (value is Guid guid)
                return guid;

            if (value is string s && Guid.TryParse(s, out Guid parsed))
                return parsed;
        }

        return null;
    }
}