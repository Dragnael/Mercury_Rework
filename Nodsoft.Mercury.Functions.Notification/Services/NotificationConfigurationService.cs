using Microsoft.EntityFrameworkCore;
using Nodsoft.Mercury.Data;
using Nodsoft.Mercury.Data.Models.Notifications;

namespace Nodsoft.Mercury.Functions.Notification.Services;

/// <summary>
/// Represents a service for handling notifications configuration.
/// </summary>
public class NotificationConfigurationService
{
	private readonly MercuryDbContext _context;

	/// <summary>
	/// Initializes a new instance of the <see cref="GetSubmittionNotificationConfigAsync"/> class.
	/// </summary>
	public NotificationConfigurationService(MercuryDbContext context)
	{
		_context = context;
	}
	
	/// <summary>
	/// Gets the configuration for notifying new form submissions.
	/// </summary>
	/// <param name="sourceId">The ID of the form source.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>The notifications configuration.</returns>
	public async Task<SubmissionNotificationOptions?> GetSubmittionNotificationConfigAsync(Guid sourceId, CancellationToken ct = default)
		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage
		=> (await _context.Sources.FirstOrDefaultAsync(s => s.Id == sourceId, ct))?.SubmissionNotificationOptions;
}