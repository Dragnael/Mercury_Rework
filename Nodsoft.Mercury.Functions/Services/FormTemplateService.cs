using Microsoft.EntityFrameworkCore;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Services;

/// <summary>
/// Represents a service for handling form templates.
/// </summary>
public sealed class FormTemplateService
{
	private readonly MercuryDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormTemplateService"/> class.
	/// </summary>
	public FormTemplateService(MercuryDbContext context)
	{
		_context = context;
	}
	
	/// <summary>
	/// Gets a form template by its ID.
	/// </summary>
	/// <param name="id">The ID of the form template.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The form template with the specified ID if found.</returns>
	public async Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) 
		=> await _context.Templates.FirstOrDefaultAsync(x => x.Id == id, ct);

	/// <summary>
	/// Gets all form templates.
	/// </summary>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>All form templates.</returns>
	public async Task<List<FormTemplate>> GetAllAsync(CancellationToken ct = default)
		=> await _context.Templates.ToListAsync(ct);

	/// <summary>
	/// Creates a new form template.
	/// </summary>
	/// <param name="template">The form template to create.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The created form template.</returns>
	public async Task<FormTemplate> CreateAsync(FormTemplate template, CancellationToken ct = default)
	{
		template.Id = Guid.CreateVersion7();
		template.PartitionKey = template.Id.ToString();
		
		_context.Templates.Add(template);
		await _context.SaveChangesAsync(ct);
		
		return template;
	}
	
	/// <summary>
	/// Updates an existing form template.
	/// </summary>
	/// <param name="template">The form template to update.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The updated form template.</returns>
	public async Task<FormTemplate> UpdateAsync(FormTemplate template, CancellationToken ct = default)
	{
		_context.Templates.Update(template);
		await _context.SaveChangesAsync(ct);
		
		return template;
	}
	
	/// <summary>
	/// Deletes a form template by its ID.
	/// </summary>
	/// <param name="id">The ID of the form template to delete.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>Whether the deletion was successful.</returns>
	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
	{
		if (await _context.Templates.FirstOrDefaultAsync(x => x.Id == id, ct) is not { } existing)
		{
			return false;
		}
		
		_context.Templates.Remove(existing);
		await _context.SaveChangesAsync(ct);
		
		return true;
	}
}