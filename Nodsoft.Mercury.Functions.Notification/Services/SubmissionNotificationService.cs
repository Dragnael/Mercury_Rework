using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Notification.Services;

/// <summary>
/// Represents a service for handling submission notifications.
/// </summary>
public sealed class SubmissionNotificationService
{
	private readonly MercuryDbContext _context;
	private readonly ILogger<SubmissionNotificationService> _logger;
	private readonly INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>[] _notificationServices;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubmissionNotificationService"/> class.
	/// </summary>
	public SubmissionNotificationService(
		MercuryDbContext context,
		ILogger<SubmissionNotificationService> logger,
		IEnumerable<INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions>> notificationServices
	)
	{
		_context = context;
		_logger = logger;
		_notificationServices = notificationServices.ToArray();
	}

	/// <summary>
	/// Notifies all configured notification services about a new form submission.
	/// </summary>
	/// <param name="notification">The submission notification request.</param>
	/// <param name="options">The notification options.</param>
	/// <param name="ignoreFail">Whether to ignore notification failures.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task NotifyAllAsync(FormSubmissionNotificationDto notification, SubmissionNotificationOptions options, bool ignoreFail = false, CancellationToken ct = default)
	{
		if (await _context.Submissions.FirstOrDefaultAsync(s => s.Id == notification.Id, ct) is not { } submission)
		{
			throw new InvalidOperationException("Form submission does not exist");
		}
		
		foreach (INotificationService<FormSubmissionNotificationDto, FormSubmission, SubmissionNotificationOptions> service in _notificationServices)
		{
			try
			{
				if (!await service.CanSendAsync(options, ct))
				{
					_logger.LogInformation("Notification service {Service} cannot send notifications", service.GetType().Name);
					continue;
				}
				
				await service.SendAsync(notification, submission, options, ct);
			}
			catch (Exception e) when (ignoreFail)
			{
				_logger.LogError(e, "Error sending notification");
			}
		}
	}
}