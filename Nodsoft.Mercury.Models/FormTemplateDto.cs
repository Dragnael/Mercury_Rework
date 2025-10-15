using JetBrains.Annotations;

namespace Nodsoft.Mercury.Models;

/// <summary>
/// Represents the DTO for a form template.
/// </summary>
public sealed record FormTemplateDto
{
	/// <summary>
	/// The ID of the form template.
	/// </summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The name of the form template.
	/// </summary>
	public required string Name { get; set; }
	
	/// <summary>
	/// The ID of the form template.
	/// </summary>
	/// <remarks>
	/// This is typically the ID of the form source.
	/// </remarks>
	/// <seealso cref="Source" />
	public Guid SourceId { get; set; }
	
	/// <summary>
	/// Whether or not the form template allows extra fields.
	/// Extra fields are fields that are not defined in the template,
	/// and aren't validated by the template.
	/// </summary>
	public bool AllowExtraFields { get; set; } = false;
	
	/// <summary>
	/// The fields of the form template.
	/// </summary>
	public List<FormTemplateFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// Represents a field of a form template.
/// </summary>
/// <param name="Path">The JSON path to the field.</param>
/// <param name="Regex">The regular expression used to validate the field.</param>
/// <param name="Required">Whether the field is required.</param>
[UsedImplicitly]
public sealed record FormTemplateFieldDto(string Path, string Regex, bool Required);