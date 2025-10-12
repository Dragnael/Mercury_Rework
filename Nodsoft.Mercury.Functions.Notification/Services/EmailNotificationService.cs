using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Models;
using TextPress;

namespace Nodsoft.Mercury.Functions.Notification.Services;

/// <summary>
/// Represents a service for handling email notifications.
/// </summary>
public sealed class EmailNotificationService :
	INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>
{
	private readonly ILogger<EmailNotificationService> _logger;
	private readonly MercuryDbContext _context;

	public EmailNotificationService(ILogger<EmailNotificationService> logger, MercuryDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	/// <summary>
	/// Sends one or more email notifications for a form submission.
	/// </summary>
	/// <param name="notification">The submission notification request.</param>
	/// <param name="submission">The form submission to notify about.</param>
	/// <param name="options">The notification options.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async ValueTask SendAsync(FormSubmissionNotificationDto notification, FormSubmission submission, SubmissionNotificationOptions options, CancellationToken ct = default)
	{
		FormSource source = await _context.Sources.FirstAsync(s => s.Id == submission.FormSourceId, ct);
		
		using (_logger.BeginScope("Sending email notifications for submission {SubmissionId}", submission.Id))
		{
			foreach (EmailNotificationOptions emailOpt in options.EmailOptions)
			{
				await SendAsync(notification, source, submission, emailOpt, ct);
			}
		}
	}

	/// <inheritdoc />
	public ValueTask<bool> CanSendAsync(SubmissionNotificationOptions config, CancellationToken ct = default) => new(config.EmailOptions is { Count: > 0 });
	
	
	private async ValueTask SendAsync(FormSubmissionNotificationDto notification, FormSource source, FormSubmission submission, EmailNotificationOptions options, CancellationToken ct = default)
	{
		Dictionary<string, string> templateDict = GetTemplateDictionary(source, submission);
		
		string subject = StringTemplate.Default.Fill(options.SubjectTemplate, templateDict);
		string body = StringTemplate.Default.Fill(options.BodyTemplate, templateDict);
		
		_logger.LogDebug("Sending email using server {SmtpServer} for submission {SubmissionId}", options.SmtpServer, submission.Id);

		IFluentEmail email = Email
			.From(options.SenderEmail, options.SenderName)
			.To(options.RecipientEmails.Select(e => new Address(e)))
			// .CC(options.CcEmails.Select(e => new Address(e)))
			.Subject(subject)
			.Header("X-Application", "NSYS Mercury")
			.Header("X-Submission-Id", submission.Id.ToString())
			.Body(body);

		email.Sender = new SmtpSender(new SmtpClient
		{
			Host = options.SmtpServer,
			Port = options.SmtpPort,
			Credentials = new NetworkCredential(options.SmtpUsername, options.SmtpPassword),
			EnableSsl = options.SmtpUseSsl
		});

		SendResponse? response = await email.SendAsync(ct);

		if (!response.Successful)
		{
			_logger.LogError("Error sending email notification for submission {SubmissionId}: {Message}", submission.Id, response.ErrorMessages);
			throw new InvalidOperationException("Error sending email notification"); 
		}
	}

	private static Dictionary<string, string> GetTemplateDictionary(FormSource source, FormSubmission submission) => new()
	{
		// Source
		{ "Source.Name", source.Name },
		{ "Source.Uri", source.Url ?? "" },
		{ "Source.ContactEmail", source.ContactEmail },
		
		// Submission
		{ "Submission.Id", submission.Id.ToString() },
		{ "Submission.FormTemplateId", submission.FormTemplateId.ToString() },
		{ "Submission.SubmittedBy", submission.SubmittedBy },
		{ "Submission.Created", submission.Created.ToString() },
		{ "Submission.Data:List:Json", JsonSerializer.Serialize(submission.Data) },
		{ "Submission.Data:List:Plain", GetPlainList(submission.Data) }
	};

	private static string GetPlainList(Dictionary<string, string> data)
	{
		StringBuilder builder = new();

		foreach ((string key, string value) in data)
		{
			builder.AppendLine($"{key} :");
			builder.AppendLine(value);
			builder.AppendLine();
		}
		
		return builder.ToString();
	}
}