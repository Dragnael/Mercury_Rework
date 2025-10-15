namespace Nodsoft.Mercury.Functions.Notification.Services;

/// <summary>
/// Specifies a service for handling notifications.
/// </summary>
public interface INotificationService<in TNotification, in TContent, in TConfig>
{
	/// <summary>
	/// Sends a notification.
	/// </summary>
	/// <param name="notification">The notification to send.</param>
	/// <param name="content">The content of the notification.</param>
	/// <param name="config">The configuration for the notification.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	ValueTask SendAsync(TNotification notification, TContent content, TConfig config, CancellationToken ct = default);

	/// <summary>
	/// Determines if one or more notifications can be sent based on the given configuration.
	/// </summary>
	/// <param name="config">The supplied configuration to be validated.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns><langword>true</langword> if the configuration permits sending valid notifications, otherwise <langword>false</langword>.</returns>
	ValueTask<bool> CanSendAsync(TConfig config, CancellationToken ct = default);
}