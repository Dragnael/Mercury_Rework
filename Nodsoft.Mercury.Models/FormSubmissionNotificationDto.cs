namespace Nodsoft.Mercury.Models;

/// <summary>
/// Represents a notification message for a form submission.
/// </summary>
public sealed record FormSubmissionNotificationDto
{
	/// <summary>
	/// The unique identifier of the submission.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	/// The unique identifier of the form source.
	/// </summary>
	public required Guid FormSourceId { get; init; }

	/// <summary>
	/// The unique identifier of the form template.
	/// </summary>
	public required Guid FormTemplateId { get; init; }

	/// <summary>
	/// The timestamp when the submission was created.
	/// </summary>
	public required DateTimeOffset Created { get; init; }

	/// <summary>
	/// The identifier of the submitter (e.g., IP address).
	/// </summary>
	public required string SubmittedBy { get; init; }
}
