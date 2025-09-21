using System.ComponentModel.DataAnnotations;

namespace Nodsoft.Mercury.Data.Data;

/// <summary>
/// Represents a source of forms within the Mercury application.
/// This typically represents an application or a module.
/// </summary>
public sealed class FormSource
{
	/// <summary>
	/// The ID of the form source.
	/// </summary>
	public required Guid Id { get; set; }
	
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

	/// <summary>
	/// All form templates owned by this form source.
	/// </summary>
	public List<FormTemplate> Templates { get; set; } = [];
}