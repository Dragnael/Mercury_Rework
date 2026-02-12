using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Submission.Services;

/// <summary>
/// Represents a service for handling form sources.
/// Optimized for performance, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormSourceService
{
    private readonly MercuryDbContext _context;
    private readonly ILogger<FormSourceService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormSourceService"/> class.
    /// </summary>
    public FormSourceService(
        MercuryDbContext context,
        ILogger<FormSourceService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the form source with the specified ID.
    /// </summary>
    public async ValueTask<FormSource?> GetFormSourceByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Sources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    /// <summary>
    /// Gets the form source behind a specified access token.
    /// </summary>
    public async ValueTask<FormSource?> GetFormSourceByTokenAsync(
        Guid token,
        CancellationToken ct = default)
    {
        if (token == Guid.Empty)
            return null;

        return await _context.AccessTokens
            .AsNoTracking()
            .Where(t => t.Id == token)
            .Select(t => t.FormSource!)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Gets all form sources.
    /// </summary>
    public async Task<List<FormSource>> GetAllFormSourcesAsync(
        CancellationToken ct = default)
    {
        return await _context.Sources
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Adds a form source to the database.
    /// </summary>
    public async Task<FormSource> AddFormSourceAsync(
        FormSource source,
        CancellationToken ct = default)
    {
        source.Id = Guid.CreateVersion7();
        source.PartitionKey = source.Id.ToString();

        _context.Sources.Add(source);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "FormSource {SourceId} created",
            source.Id);

        return source;
    }

    /// <summary>
    /// Updates a form source in the database.
    /// </summary>
    public async Task<FormSource> UpdateFormSourceAsync(
        FormSource source,
        CancellationToken ct = default)
    {
        _context.Sources.Update(source);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "FormSource {SourceId} updated",
            source.Id);

        return source;
    }

    /// <summary>
    /// Deletes a form source from the database.
    /// </summary>
    public async Task<bool> DeleteFormSourceAsync(
        Guid sourceId,
        CancellationToken ct = default)
    {
        if (sourceId == Guid.Empty)
            return false;

        var existing = await _context.Sources
            .FirstOrDefaultAsync(s => s.Id == sourceId, ct);

        if (existing is null)
            return false;

        _context.Sources.Remove(existing);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "FormSource {SourceId} deleted",
            sourceId);

        return true;
    }
}
