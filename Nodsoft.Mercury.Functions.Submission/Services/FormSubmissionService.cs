using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Submission.Services;

/// <summary>
/// Represents a service for handling form submissions.
/// Optimized for performance, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormSubmissionService
{
    private readonly MercuryDbContext _context;
    private readonly ILogger<FormSubmissionService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormSubmissionService"/> class.
    /// </summary>
    public FormSubmissionService(
        MercuryDbContext context,
        ILogger<FormSubmissionService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form submission by its ID.
    /// </summary>
    public async Task<FormSubmission?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    /// <summary>
    /// Gets all form submissions from a given form source.
    /// </summary>
    public IQueryable<FormSubmission> GetAllForSource(Guid sourceId)
    {
        return _context.Submissions
            .AsNoTracking()
            .Where(s => s.FormSourceId == sourceId);
    }

    /// <summary>
    /// Adds a form submission to the database.
    /// </summary>
    public async Task<FormSubmission> AddAsync(
        FormSubmission submission,
        CancellationToken ct = default)
    {
        if (submission.AccessTokenId == Guid.Empty)
            throw new InvalidOperationException("Access token is required.");

        var now = DateTimeOffset.UtcNow;

        // Validate token + expiration + revoked in single query
        var tokenInfo = await _context.AccessTokens
            .AsNoTracking()
            .Where(t => t.Id == submission.AccessTokenId &&
                        !t.Revoked &&
                        (t.Expires == null || t.Expires > now))
            .Select(t => t.FormSourceId)
            .FirstOrDefaultAsync(ct);

        if (tokenInfo == Guid.Empty)
            throw new InvalidOperationException("Invalid or expired access token.");

        submission.FormSourceId = tokenInfo;
        submission.Id = Guid.CreateVersion7();
        submission.PartitionKey = tokenInfo.ToString();

        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Submission {SubmissionId} created for Source {SourceId}",
            submission.Id,
            tokenInfo);

        return submission;
    }

    /// <summary>
    /// Updates a form submission in the database.
    /// </summary>
    public async Task<FormSubmission> UpdateAsync(
        FormSubmission submission,
        CancellationToken ct = default)
    {
        _context.Submissions.Update(submission);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Submission {SubmissionId} updated",
            submission.Id);

        return submission;
    }

    /// <summary>
    /// Deletes a form submission from the database.
    /// </summary>
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return false;

        var existing = await _context.Submissions
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (existing is null)
            return false;

        _context.Submissions.Remove(existing);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Submission {SubmissionId} deleted",
            id);

        return true;
    }
}
