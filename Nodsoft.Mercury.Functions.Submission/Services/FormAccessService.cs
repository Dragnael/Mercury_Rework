using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Submission.Services;

/// <summary>
/// Represents a service for handling form access, including access tokens.
/// Optimized for performance, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormAccessService
{
    private readonly MercuryDbContext _context;
    private readonly ILogger<FormAccessService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormAccessService"/> class.
    /// </summary>
    public FormAccessService(
        MercuryDbContext context,
        ILogger<FormAccessService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form access token by its ID.
    /// </summary>
    public async Task<FormAccessToken?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return await _context.AccessTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Determines if a form access token can be used to access a form source.
    /// </summary>
    /// <returns>
    /// The form source ID for which this token is valid,
    /// or <see langword="null"/> if invalid or expired.
    /// </returns>
    public async Task<Guid?> CanAccessSourceAsync(
        Guid tokenId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var result = await _context.AccessTokens
            .AsNoTracking()
            .Where(t => t.Id == tokenId &&
                        !t.Revoked &&
                        (t.Expires == null || t.Expires > now))
            .Select(t => t.FormSourceId)
            .FirstOrDefaultAsync(ct);

        return result == Guid.Empty ? null : result;
    }

    /// <summary>
    /// Creates a new form access token.
    /// </summary>
    public async Task<FormAccessToken> CreateAsync(
        Guid sourceId,
        DateTimeOffset? expires = null,
        CancellationToken ct = default)
    {
        // Validate source existence without loading full entity
        var sourceExists = await _context.Sources
            .AsNoTracking()
            .AnyAsync(s => s.Id == sourceId, ct);

        if (!sourceExists)
        {
            throw new ArgumentException("Invalid source ID.", nameof(sourceId));
        }

        var token = new FormAccessToken
        {
            Id = Guid.CreateVersion7(), // time-ordered, DB friendly
            PartitionKey = sourceId.ToString(),
            FormSourceId = sourceId,
            Expires = expires,
            Revoked = false,
            Created = DateTimeOffset.UtcNow
        };

        _context.AccessTokens.Add(token);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Access token {TokenId} created for Source {SourceId}",
            token.Id,
            sourceId);

        return token;
    }

    /// <summary>
    /// Revokes a form access token.
    /// </summary>
    public async Task<bool> RevokeAsync(
        Guid tokenId,
        CancellationToken ct = default)
    {
        var token = await _context.AccessTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, ct);

        if (token is null)
            return false;

        if (token.Revoked)
            return true; // already revoked

        token.Revoked = true;

        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Access token {TokenId} revoked",
            tokenId);

        return true;
    }
}
