using TextPress;

namespace Nodsoft.Mercury.Data.Models.Notifications;

/// <summary>
/// Defines options for sending email notifications.
/// </summary>
public sealed record NotificationEmailOptions
{
	/// <summary>
	/// The SMTP server to use for sending email notifications.
	/// </summary>
	public required string SmtpServer { get; init; }
	
	/// <summary>
	/// The port to use for sending email notifications.
	/// </summary>
	public required ushort SmtpPort { get; init; }
	
	/// <summary>
	/// The username to use for sending email notifications.
	/// </summary>
	public required string SmtpUsername { get; init; }
	
	/// <summary>
	/// The password to use for sending email notifications.
	/// </summary>
	public required string SmtpPassword { get; init; }
	
	/// <summary>
	/// The sender email address to use for sending email notifications.
	/// </summary>
	public required string SenderEmail { get; init; }

	/// <summary>
	/// The sender name to use for sending email notifications.
	/// </summary>
	public string SenderName { get; init; } = "NSYS Mercury";

	/// <summary>
	/// The subject template to use for sending email notifications.
	/// Uses TextPress to render the template.
	/// </summary>
	public string SubjectTemplate { get; init; }

	/// <summary>
	/// The body template to use for sending email notifications.
	/// Uses TextPress to render the template.
	/// </summary>
	public string BodyTemplate { get; init; } = DefaultPlainTextTemplate;
	
	/// <summary>
	/// The HTML body template to use for sending email notifications.
	/// Uses TextPress to render the template.
	/// </summary>
	/// <remarks>
	/// Specifying this template instead of <see cref="BodyTemplate"/> will cause this template to be used for HTML emails instead.
	/// </remarks>
	public string? HtmlBodyTemplatePath { get; init; }

	/// <summary>
	/// The email addresses to send the notification to.
	/// </summary>
	public string[] RecipientEmails { get; init; } = [];

	/// <summary>
	/// The email addresses to CC the notification to.
	/// </summary>
	public string[] CcEmails { get; init; } = [];
	
	#region PlainTextTemplate

	/// <summary>
	/// The default plain text template to use for sending email notifications.
	/// </summary>
	public const string DefaultPlainTextTemplate =
		"""
		Greetings,
		
		A new form was submitted to the {Source.Name} form source at {Source.Uri}.
		Please find attached the submitted information :
		
		---
		{Submission.Data:List:Plain}
		---
		
		With regards,
		
		NSYS Mercury
		https://nodsoft.net
		
		
		PS: This is an automated message. Please do not reply at this message's sender address. 
		For support inquiries, please contact us at admin@nodsoft.net.
		""";
	
	#endregion // PlainTextTemplate
}