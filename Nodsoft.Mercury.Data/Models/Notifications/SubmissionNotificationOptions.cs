namespace Nodsoft.Mercury.Data.Models.Notifications;

/// <summary>
/// Defines options for form submission notifications.
/// </summary>
public sealed record SubmissionNotificationOptions
{
	/// <summary>
	/// The email options to use for sending notifications.
	/// </summary>
	public List<NotificationEmailOptions> EmailOptions { get; set; } = [];
}