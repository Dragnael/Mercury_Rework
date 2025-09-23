using System.Net;

namespace Nodsoft.Mercury.Data.Data;

/// <summary>
/// Represents a form submission. This 
/// </summary>
public sealed class FormSubmission
{
	/// <summary>
	/// The ID of the form submission.
	/// </summary>
	public required Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The form template ID.
	/// </summary>
	/// <seealso cref="FormTemplate" />
	public required Guid FormTemplateId { get; set; }
	
	/// <summary>
	/// The form template associated with this submission.
	/// </summary>
	/// <seealso cref="FormTemplateId" />
	public required FormTemplate FormTemplate { get; set; }
	
	/// <summary>
	/// The data of the form submission.
	/// </summary>
	public required Dictionary<string, object> Data { get; set; } = new();
	
	/// <summary>
	/// The timestamp of the submission.
	/// </summary>
	public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
	
	/// <summary>
	/// The IP address of the submitter.
	/// </summary>
	public required IPAddress SubmittedBy { get; set; }
	
	/// <summary>
	/// The ID of the Access Token used for this submission.
	/// </summary>
	/// <seealso cref="AccessToken" />
	public required Guid AccessTokenId { get; set; }
	
	/// <summary>
	/// The Access Token used for this submission.
	/// </summary>
	/// <seealso cref="AccessTokenId" />
	public required FormAccessToken AccessToken { get; set; }
}