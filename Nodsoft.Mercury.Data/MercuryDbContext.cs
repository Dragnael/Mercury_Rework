using Microsoft.EntityFrameworkCore;
using Nodsoft.Mercury.Data.Data;

namespace Nodsoft.Mercury.Data;

public sealed class MercuryDbContext : DbContext
{ 
	/// <summary>
	/// The form sources in the database.
	/// </summary>
	public DbSet<FormSource> Sources { get; init; }
	
	/// <summary>
	/// The form templates in the database.
	/// </summary>
	public DbSet<FormTemplate> Templates { get; init; }
	
	/// <summary>
	/// The form submissions in the database.
	/// </summary>
	public DbSet<FormSubmission> Submissions { get; init; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MercuryDbContext"/> class.
	/// </summary>
	/// <param name="options">The options for this context.</param>
	public MercuryDbContext(DbContextOptions<MercuryDbContext> options) : base(options) { }
}