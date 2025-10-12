using System.ComponentModel.DataAnnotations;
using Nodsoft.Mercury.Data.Models.Notifications;

namespace Nodsoft.Mercury.Data.Models;

/// <summary>
/// Represents a source of forms within the Mercury application.
/// This typically represents an application or a module.
/// </summary>
public sealed record FormSource : CosmosModelBase<Guid>
{
	/// <summary>
	/// The ID of the form source.
	/// </summary>
	public override required Guid Id { get; set; } = Guid.CreateVersion7();
	
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
	
	/// <summary>
	/// The access tokens for this form source.
	/// </summary>
	public List<FormAccessToken> AccessTokens { get; set; } = [];
	
	/// <summary>
	/// The notification options for each new related form submission.
	/// </summary>
	public SubmissionNotificationOptions SubmissionNotificationOptions { get; set; } = new();
}