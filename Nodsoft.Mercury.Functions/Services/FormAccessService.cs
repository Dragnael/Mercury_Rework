using Microsoft.EntityFrameworkCore;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

// ReSharper disable EntityFramework.NPlusOne.IncompleteDataUsage
// ReSharper disable EntityFramework.NPlusOne.IncompleteDataQuery

namespace Nodsoft.Mercury.Functions.Services;

/// <summary>
/// Represents a service for handling form access, including access tokens. 
/// </summary>
public sealed class FormAccessService
{
	private readonly MercuryDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormAccessService"/> class.
	/// </summary>
	public FormAccessService(MercuryDbContext context)
	{
		_context = context;
	}
	
	/// <summary>
	/// Gets a form access token by its ID.
	/// </summary>
	/// <param name="id">The ID of the form access token.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The form access token with the specified ID if found.</returns>
	public async Task<FormAccessToken?> GetByIdAsync(Guid id, CancellationToken ct = default) 
		=> await _context.AccessTokens.FirstOrDefaultAsync(t => t.Id == id, ct);

	/// <summary>
	/// Determines if a form access token can be used to access a form source.
	/// </summary>
	/// <param name="tokenId">The access token.</param>
	/// <param name="sourceId">The ID of the form source.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>True if the token can be used to access the source; otherwise, false.</returns>
	public async Task<bool> CanAccessSourceAsync(Guid tokenId, Guid sourceId, CancellationToken ct = default)
		=> await _context.AccessTokens.FirstOrDefaultAsync(s => s.Id == tokenId, ct) is { Revoked: false } token
			&& token.FormSourceId == sourceId
			&& (token.Expires is null || token.Expires > DateTimeOffset.UtcNow)
			&& await _context.Sources.AnyAsync(t => t.Id == sourceId, ct);


	/// <summary>
	/// Creates a new form access token.
	/// </summary>
	/// <param name="sourceId">The ID of the form source.</param>
	/// <param name="expires">The optional expiration date of the token.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The created form access token.</returns>
	public async Task<FormAccessToken> CreateAsync(Guid sourceId, DateTimeOffset? expires = null, CancellationToken ct = default)
	{
		if (await _context.Sources.FirstOrDefaultAsync(s => s.Id == sourceId, ct) is not { } source)
		{
			throw new ArgumentException("Invalid source ID.", nameof(sourceId));
		}
		
		FormAccessToken token = new()
		{
			Id = Guid.CreateVersion7(),
			PartitionKey = sourceId.ToString(),
			FormSourceId = sourceId,
			FormSource = null!,
			Expires = expires,
			Revoked = false,
			Created = DateTimeOffset.UtcNow
		};
		
		source.AccessTokens.Add(token);
		await _context.SaveChangesAsync(ct);
		
		return token;
	}

	/// <summary>
	/// Revokes a form access token.
	/// </summary>
	/// <param name="tokenId">The ID of the token to revoke.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>True if the token was successfully revoked; otherwise, false.</returns>
	public async Task<bool> RevokeAsync(Guid tokenId, CancellationToken ct = default)
	{
		if (await GetByIdAsync(tokenId, ct) is not { } token)
		{
			return false;
		}
		
		token.Revoked = true;
		await _context.SaveChangesAsync(ct);
		
		return true;
	}
}