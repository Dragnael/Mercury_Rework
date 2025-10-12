namespace Nodsoft.Mercury.Data.Models.Notifications;

/// <summary>
/// Represents options for sending Discord webhook notifications.
/// </summary>
public sealed class DiscordWebhookNotificationOptions
{
	/// <summary>
	/// The URI of the Discord webhook notifications are sent to.
	/// </summary>
	public required string WebhookUri { get; set; }

	/// <summary>
	/// The name of the sender for the Discord webhook notifications.
	/// </summary>
	public string SenderName { get; set; } = "NSYS Mercury";

	/// <summary>
	/// The avatar URL of the sender for the Discord webhook notifications.
	/// </summary>
	public string SenderIconUri { get; set; } = "https://nodsoft.net/logo.png";
}