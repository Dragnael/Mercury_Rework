using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Nodsoft.Mercury.Data.Extensions;
using Nodsoft.Mercury.Data.Models;

namespace Nodsoft.Mercury.Data;

public sealed class MercuryDbContext : DbContext
{ 
	/// <summary>
	/// The form sources in the database.
	/// </summary>
	public DbSet<FormSource> Sources { get; init; }
	
	/// <summary>
	/// The form access tokens in the database.
	/// </summary>
	public DbSet<FormAccessToken> AccessTokens { get; init; }
	
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
			entity.IsCosmosEntity<FormSource, Guid>(nameof(Sources));
		});

		modelBuilder.Entity<FormAccessToken>(entity =>
		{
			entity.IsCosmosEntity<FormAccessToken, Guid>(nameof(AccessTokens));
		});
		
		modelBuilder.Entity<FormTemplate>(entity =>
		{
			entity.IsCosmosEntity<FormTemplate, Guid>(nameof(Templates));
		});
		
		modelBuilder.Entity<FormSubmission>(entity =>
		{
			entity.IsCosmosEntity<FormSubmission, Guid>(nameof(Submissions));
		});
	}
}