using Microsoft.EntityFrameworkCore;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Functions.Submission.Services;

/// <summary>
/// Represents a service for handling form submissions.
/// </summary>
public sealed class FormSubmissionService
{
	private readonly MercuryDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormSubmissionService"/> class.
	/// </summary>
	public FormSubmissionService(MercuryDbContext context)
	{
		_context = context;
	}
	
	/// <summary>
	/// Gets a form submission by its ID.
	/// </summary>
	/// <param name="id">The ID of the form submission.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The form submission with the specified ID if found.</returns>
	public async Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default) 
		=> await _context.Submissions.FirstOrDefaultAsync(s => s.Id == id, ct);
	
	/// <summary>
	/// Gets all form submissions from a given form source.
	/// </summary>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>All form submissions.</returns>
	public IQueryable<FormSubmission> GetAllForSource(Guid sourceId, CancellationToken ct = default)
		=> _context.Submissions.Where(s => s.FormSourceId == sourceId);
	
	/// <summary>
	/// Adds a form submission to the database.
	/// </summary>
	/// <param name="submission">The form submission to add.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The added form submission.</returns>
	public async Task<FormSubmission> AddAsync(FormSubmission submission, CancellationToken ct = default)
	{
		// Grab the form source to ensure it exists.
		if (await _context.AccessTokens.FirstAsync(t => t.Id == submission.AccessTokenId, ct) is not { } token ||
			await _context.Sources.FirstOrDefaultAsync(s => s.Id == token.FormSourceId, ct) is not { } source)
		{
			throw new InvalidOperationException("Form source does not exist.");
		}
		
		submission.FormSourceId = source.Id;
		submission.Id = Guid.CreateVersion7();
		submission.PartitionKey = submission.FormSourceId.ToString();
		
		_context.Submissions.Add(submission);
		await _context.SaveChangesAsync(ct);
		
		return submission;
	}
	
	/// <summary>
	/// Updates a form submission in the database.
	/// </summary>
	/// <param name="submission">The form submission to update.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The updated form submission.</returns>
	public async Task<FormSubmission> UpdateAsync(FormSubmission submission, CancellationToken ct = default)
	{
		_context.Submissions.Update(submission);
		await _context.SaveChangesAsync(ct);
		
		return submission;
	}
	
	/// <summary>
	/// Deletes a form submission from the database.
	/// </summary>
	/// <param name="id">The ID of the form submission to delete.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>Whether the deletion was successful.</returns>
	public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
	{
		if (await _context.Submissions.FirstOrDefaultAsync(s => s.Id == id, ct) is not { } existing)
		{
			return false;
		}
		
		_context.Submissions.Remove(existing);
		await _context.SaveChangesAsync(ct);
		
		return true;
	}
}