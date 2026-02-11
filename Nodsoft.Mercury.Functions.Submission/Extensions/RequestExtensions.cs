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
		{
			if (ctx.Items.TryGetValue(AccessTokenMiddleware.AccessTokenClaim, out object? accessTokenId))
			{
				if (accessTokenId is Guid id)
					return id;

				if (accessTokenId is Guid? nullableId)
					return nullableId;
			}

			return null;
		}

		/// <summary>
		/// Gets the ID of the form source associated with the request.
		/// </summary>
		/// <returns>The ID of the form source, or <see langword="null"/> if not found.</returns>
		public Guid? GetSourceId()
		{
			if (ctx.Items.TryGetValue(AccessTokenMiddleware.SourceIdClaim, out object? sourceId))
			{
				if (sourceId is Guid id)
					return id;

				if (sourceId is Guid? nullableId)
					return nullableId;
			}

			return null;
		}
	}
}