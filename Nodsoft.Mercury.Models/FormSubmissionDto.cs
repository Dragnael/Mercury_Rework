using System.Net;

namespace Nodsoft.Mercury.Models;

public class FormSubmissionDto
{
	/// <summary>
	/// The ID of the form submission.
	/// </summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();
	
	/// <summary>
	/// The form template ID.
	/// </summary>
	/// <seealso cref="FormTemplateDto" />
	public required Guid FormTemplateId { get; set; }
	
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
	public required string SubmittedBy { get; set; }
	
	/// <summary>
	/// The ID of the Access Token used for this submission.
	/// </summary>
	/// <seealso cref="AccessToken" />
	public required Guid AccessTokenId { get; set; }
}