namespace Nodsoft.Mercury.Data.Models.Notifications;

/// <summary>
/// Defines options for form submission notifications.
/// </summary>
public sealed record SubmissionNotificationOptions
{
	/// <summary>
	/// The email options to use for sending notifications.
	/// </summary>
	public List<EmailNotificationOptions> EmailOptions { get; set; } = [];

	/// <summary>
	/// The Discord Webhook configurations to use for sending notifications.
	/// </summary>
	public List<DiscordWebhookNotificationOptions> DiscordWebhookOptions { get; set; } = [];
}