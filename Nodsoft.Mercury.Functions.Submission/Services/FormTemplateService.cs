using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Submission.Services;

/// <summary>
/// Represents a service for handling form templates.
/// Optimized for performance, Azure-ready and Aspire compatible.
/// </summary>
public sealed class FormTemplateService
{
    private readonly MercuryDbContext _context;
    private readonly ILogger<FormTemplateService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormTemplateService"/> class.
    /// </summary>
    public FormTemplateService(
        MercuryDbContext context,
        ILogger<FormTemplateService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a form template by its ID.
    /// </summary>
    public async Task<FormTemplate?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return null;

        return await _context.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <summary>
    /// Gets all form templates.
    /// </summary>
    public async Task<List<FormTemplate>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await _context.Templates
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Creates a new form template.
    /// </summary>
    public async Task<FormTemplate> CreateAsync(
        FormTemplate template,
        CancellationToken ct = default)
    {
        template.Id = Guid.CreateVersion7();
        template.PartitionKey = template.Id.ToString();

        _context.Templates.Add(template);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Template {TemplateId} created",
            template.Id);

        return template;
    }

    /// <summary>
    /// Updates an existing form template.
    /// </summary>
    public async Task<FormTemplate> UpdateAsync(
        FormTemplate template,
        CancellationToken ct = default)
    {
        _context.Templates.Update(template);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Template {TemplateId} updated",
            template.Id);

        return template;
    }

    /// <summary>
    /// Deletes a form template by its ID.
    /// </summary>
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return false;

        var existing = await _context.Templates
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existing is null)
            return false;

        _context.Templates.Remove(existing);
        await _context.SaveChangesAsync(ct);

        _logger?.LogInformation(
            "Template {TemplateId} deleted",
            id);

        return true;
    }
}
