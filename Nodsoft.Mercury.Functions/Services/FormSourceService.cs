using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Services;

/// <summary>
/// Represents a service for handling form sources.
/// </summary>
public sealed class FormSourceService
{
	private readonly MercuryDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormSourceService"/> class.
	/// </summary>
	public FormSourceService(MercuryDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Gets the form source with the specified token ID.
	/// </summary>
	/// <param name="id">The ID of the form source.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The form source, or <see langword="null"/> if not found.</returns>
	public async ValueTask<FormSource?> GetFormSourceByIdAsync(Guid id, CancellationToken ct = default) 
		=> await _context.Sources.FirstOrDefaultAsync(s => s.Id == id, ct);

	/// <summary>
	/// Gets the form source behind a specified access token.
	/// </summary>
	/// <param name="token">The access token.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The form source, or <see langword="null"/> if not found.</returns>
	public async ValueTask<FormSource?> GetFormSourceByTokenAsync(Guid token, CancellationToken ct = default) 
		=> await _context.Sources.FirstOrDefaultAsync(s => s.AccessTokens.Any(t => t.Id == token), ct);

	/// <summary>
	/// Gets all form sources.
	/// </summary>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>All form sources.</returns>
	public async Task<List<FormSource>> GetAllFormSourcesAsync(CancellationToken ct = default)
		=> await _context.Sources.ToListAsync(ct);

	/// <summary>
	/// Adds a form source to the database.
	/// </summary>
	/// <param name="source">The form source to add.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The added form source.</returns>
	public async Task<FormSource> AddFormSourceAsync(FormSource source, CancellationToken ct = default)
	{
		source.Id = Guid.CreateVersion7();
		source.PartitionKey = source.Id.ToString();
		
		_context.Sources.Add(source);
		await _context.SaveChangesAsync(ct);

		return source;
	}
	
	/// <summary>
	/// Updates a form source in the database.
	/// </summary>
	/// <param name="source">The form source to update.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The updated form source.</returns>
	public async Task<FormSource> UpdateFormSourceAsync(FormSource source, CancellationToken ct = default)
	{
		_context.Sources.Update(source);
		await _context.SaveChangesAsync(ct);
		return source;
	}

	/// <summary>
	/// Deletes a form source from the database.
	/// </summary>
	/// <param name="sourceId">The ID of the form source to delete.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>Whether the deletion was successful.</returns>
	public async Task<bool> DeleteFormSourceAsync(Guid sourceId, CancellationToken ct = default)
	{
		if (await _context.Sources.Where(s => s.Id == sourceId).FirstOrDefaultAsync(ct) is not { } existing)
		{
			return false;
		}
		
		EntityEntry<FormSource> entry = _context.Sources.Remove(existing);
		await _context.SaveChangesAsync(ct);

		return true;
	}
}