using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Nodsoft.Mercury.Data.Models.Notifications;
using Nodsoft.Mercury.Functions.Notification.Services;
using Nodsoft.Mercury.Models;

namespace Nodsoft.Mercury.Functions.Notification;

/// <summary>
/// Represents functions for notifying on form events.
/// </summary>
public sealed class NotificationFunctions
{
	private readonly NotificationConfigurationService _configService;

	public NotificationFunctions(NotificationConfigurationService configService)
	{
		_configService = configService;
	}
	
	[Function(nameof(OnFormSubmissionReceivedAsync))]
	public async Task OnFormSubmissionReceivedAsync(
		FunctionContext ctx,
		[ServiceBusTrigger("submissions", Connection = "notifications-mq")] ServiceBusReceivedMessage sbMessage
	) {
		FormSubmissionNotificationDto? notification = sbMessage.Body.ToObjectFromJson<FormSubmissionNotificationDto>();
		
		if (await _configService.GetSubmittionNotificationConfigAsync(notification.FormSourceId, ctx.CancellationToken) is not { } config)
		{
			return;
		}
		
		await using AsyncServiceScope scope = ctx.InstanceServices.CreateAsyncScope();
		SubmissionNotificationService service = scope.ServiceProvider.GetRequiredService<SubmissionNotificationService>();
		
		await service.NotifyAllAsync(notification, config, ct: ctx.CancellationToken);
	}
}