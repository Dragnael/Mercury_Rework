using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Models;
using TextPress;

namespace Nodsoft.Mercury.Functions.Notification.Services;

public sealed class FreeMobileSelfNotificationService :
	INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>
{
	private readonly ILogger<FreeMobileSelfNotificationService> _logger;
	private readonly MercuryDbContext _context;
	private readonly HttpClient _httpClient;

	public FreeMobileSelfNotificationService(ILogger<FreeMobileSelfNotificationService> logger, MercuryDbContext context, HttpClient httpClient)
	{
		_logger = logger;
		_context = context;

		httpClient.BaseAddress = new("https://smsapi.free-mobile.fr/sendmsg");
		_httpClient = httpClient;
	}

	public async ValueTask SendAsync(FormSubmissionNotificationDto notification, FormSubmission submission, SubmissionNotificationOptions options, CancellationToken ct = default)
	{
		FormSource source = await _context.Sources.FirstAsync(s => s.Id == submission.FormSourceId, ct);

		using (_logger.BeginScope("Sending Discord Webhook notifications for submission {SubmissionId}", submission.Id))
		{
			foreach (FreeMobileSelfNotificationOptions freeMobileSelfOpt in options.FreeMobileSelfOptions)
			{
				await SendAsync(notification, source, submission, freeMobileSelfOpt, ct);
			}
		}
	}

	public ValueTask<bool> CanSendAsync(SubmissionNotificationOptions config, CancellationToken ct = default) => new(config.FreeMobileSelfOptions is { Count: > 0 });

	private async Task SendAsync(FormSubmissionNotificationDto notification, FormSource source, FormSubmission submission, FreeMobileSelfNotificationOptions options, CancellationToken ct)
	{
		FreeMobileSelfNotificationDto dto = new()
		{
			UserId = options.UserId,
			ApiKey = options.ApiKey,
			Message = StringTemplate.Default.Fill(options.MessageTemplate, GetTemplateDictionary(source, submission))
		};
		
		using HttpRequestMessage request = new(HttpMethod.Post, "");
		request.Content = JsonContent.Create(dto);
		
		HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();
		_logger.LogInformation("Successfully sent FreeMobile Self-SMS notification to Account ID {AccountId} for submission {SubmissionId}", options.UserId, submission.Id);
	}

	public sealed class FreeMobileSelfNotificationDto
	{
		[JsonPropertyName("user")]
		public required int UserId { get; set; }

		[JsonPropertyName("pass")]
		public required string ApiKey { get; set; }

		[JsonPropertyName("msg")]
		public required string Message { get; set; }
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