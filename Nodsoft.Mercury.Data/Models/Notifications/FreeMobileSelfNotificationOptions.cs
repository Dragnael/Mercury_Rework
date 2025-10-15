namespace Nodsoft.Mercury.Data.Models.Notifications;

/// <summary>
/// Represents options for sending notifications via Free Mobile's Self-sent-SMS system.
/// </summary>
/// <remarks>
/// This option should be used as a pager notification.
/// Use it in conjunction with email and/or Discord notifications for the full form.
/// </remarks>
/// <seealso href="https://mobile.free.fr/account/mes-options/notifications-sms" />
public class FreeMobileSelfNotificationOptions
{
	/// <summary>
	/// The user ID of the Free Mobile account to send notifications from.
	/// </summary>
	public required int UserId { get; set; }
	
	/// <summary>
	/// The API key used to authenticate with Free Mobile.
	/// </summary>
	public required string ApiKey { get; set; }
	
	/// <summary>
	/// The message template to use for sending notifications.
	/// </summary>
	public required string MessageTemplate { get; set; }


	public const string DefaultMessageTemplate =
		"""
		Hi,
		A new form was submitted to {Source.Name}.
		Submission ID: {Submission.Id}
		Check your emails for more details.
		- NSYS Mercury
		""";
}