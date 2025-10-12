using Discord;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Notification.Services;

/// <summary>
/// Represents a service for handling notifications via a Discord webhook.
/// </summary>
public class DiscordWebhookNotificationService : 
	INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>
{
	private readonly ILogger<DiscordWebhookNotificationService> _logger;
	private readonly MercuryDbContext _context;

	public DiscordWebhookNotificationService(ILogger<DiscordWebhookNotificationService> logger, MercuryDbContext context)
	{
		_logger = logger;
		_context = context;
	}
	
	public async ValueTask SendAsync(FormSubmissionNotificationDto notification, FormSubmission submission, SubmissionNotificationOptions options, CancellationToken ct = default)
	{
		FormSource source = await _context.Sources.FirstAsync(s => s.Id == submission.FormSourceId, ct);
		
		using (_logger.BeginScope("Sending Discord Webhook notifications for submission {SubmissionId}", submission.Id))
		{
			foreach (DiscordWebhookNotificationOptions discordWebhookOpt in options.DiscordWebhookOptions)
			{
				await SendAsync(notification, source, submission, discordWebhookOpt, ct);
			}
		}
	}

	private async Task SendAsync(
		FormSubmissionNotificationDto notification,
		FormSource source,
		FormSubmission submission,
		DiscordWebhookNotificationOptions options,
		CancellationToken ct = default
	) {
		_logger.LogDebug("Sending Discord Webhook notification for submission {SubmissionId}", submission.Id);

		using DiscordWebhookClient client = new(options.WebhookUri);

		EmbedBuilder embedBuilder = new EmbedBuilder()
			.WithTitle($"New form submitted to {source.Name}")
			.WithDescription($"A new form was submitted to the source at {source.Url}.\r\nSubmission ID : `{submission.Id}`")
			.WithAuthor(options.SenderName, options.SenderIconUri)
			.WithColor(Color.Green)
			.WithTimestamp(submission.Created)
			.WithFooter($"NSYS Mercury — Powered by Nodsoft Systems", "https://nodsoft.net/logo.png");

		foreach ((string key, string value) in submission.Data)
		{
			embedBuilder.AddField(key, value, inline: false);
		}
		
		await client.SendMessageAsync(embeds: [embedBuilder.Build()]);
	}

	public ValueTask<bool> CanSendAsync(SubmissionNotificationOptions config, CancellationToken ct = default) => new(config.DiscordWebhookOptions is { Count: > 0 });
}