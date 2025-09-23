using System.ComponentModel.DataAnnotations;

namespace Nodsoft.Mercury.Models;

/// <summary>
/// Represents the DTO for a source of forms within the Mercury application.
/// </summary>
public sealed record FormSourceDto
{
	/// <summary>
	/// The ID of the form source.
	/// </summary>
	public required Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The name of the form source.
	/// </summary>
	/// <remarks>
	/// This is typically the name of the application or module.
	/// </remarks>
	[StringLength(256)]
	public required string Name { get; set; }
	
	/// <summary>
	/// The URL of the form source.
	/// </summary>
	[Url]
	public string? Url { get; set; }
	
	/// <summary>
	/// A contact email address for the form source.
	/// </summary>
	[EmailAddress]
	public required string ContactEmail { get; set; }
}