using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Nodsoft.Mercury.Data.Models;

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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		
		modelBuilder.Entity<FormSource>(entity =>
		{
			entity.ToContainer(nameof(Sources));
			entity.HasPartitionKey(e => e.PartitionKey);
			entity.UseETagConcurrency()
		});
		
		modelBuilder.Entity<FormTemplate>(entity =>
		{
			entity.ToContainer(nameof(Templates));
			entity.HasPartitionKey(e => e.PartitionKey);
		});
		
		modelBuilder.Entity<FormSubmission>(entity =>
		{
			entity.ToContainer(nameof(Submissions));
			entity.HasPartitionKey(e => e.PartitionKey);
		});
	}
}