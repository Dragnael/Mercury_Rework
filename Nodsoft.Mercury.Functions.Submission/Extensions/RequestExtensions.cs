using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Nodsoft.Mercury.Functions.Submission.Services.Middlewares;

namespace Nodsoft.Mercury.Functions.Submission.Extensions;

/// <summary>
/// Provides extensions for working with HTTP requests made to Azure Functions.
/// </summary>
public static class RequestExtensions
{
	extension(FunctionContext ctx)
	{
		/// <summary>
		/// Gets the ID of the access token associated with the request.
		/// </summary>
		/// <returns>The ID of the access token, or <see langword="null"/> if not found.</returns>
		public Guid? GetAccessTokenId()
			=> ctx.Items.TryGetValue(AccessTokenMiddleware.AccessTokenClaim, out object? accessTokenId) 
			? accessTokenId as Guid?
			: null;

		/// <summary>
		/// Gets the ID of the form source associated with the request.
		/// </summary>
		/// <returns>The ID of the form source, or <see langword="null"/> if not found.</returns>
		public Guid? GetSourceId()
			=> ctx.Items.TryGetValue(AccessTokenMiddleware.SourceIdClaim, out object? sourceId)
				? sourceId as Guid?
				: null;
	}
}