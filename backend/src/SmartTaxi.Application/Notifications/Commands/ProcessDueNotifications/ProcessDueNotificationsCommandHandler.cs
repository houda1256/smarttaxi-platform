using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;

namespace SmartTaxi.Application.Notifications.Commands.ProcessDueNotifications;

/// <summary>
/// Idempotent: NotificationDispatcher.DispatchAsync itself dedupes on
/// (SourceType, SourceId, RecipientUserId, Category), so calling this sweep
/// twice on the same due ScheduledNotification never double-notifies. Each row
/// is marked Processed regardless of the dispatch outcome — the dispatcher
/// never throws and already persists its own failure detail per channel (see
/// NotificationDeliveryAttempt), so "processed" here means "handed off",
/// not "delivered".
/// </summary>
public sealed class ProcessDueNotificationsCommandHandler : ICommandHandler<ProcessDueNotificationsCommand, Result<int>>
{
    private readonly IScheduledNotificationRepository _scheduledNotificationRepository;
    private readonly INotificationDispatcher _dispatcher;

    public ProcessDueNotificationsCommandHandler(
        IScheduledNotificationRepository scheduledNotificationRepository, INotificationDispatcher dispatcher)
    {
        _scheduledNotificationRepository = scheduledNotificationRepository;
        _dispatcher = dispatcher;
    }

    public async Task<Result<int>> Handle(ProcessDueNotificationsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var due = await _scheduledNotificationRepository.GetDueAsync(utcNow, cancellationToken);

        var processedCount = 0;

        foreach (var scheduled in due)
        {
            await _dispatcher.DispatchAsync(
                new NotificationRequest(
                    scheduled.RecipientUserId, scheduled.Category, scheduled.TemplateKey, scheduled.Variables,
                    scheduled.IsMandatory, scheduled.SourceType, scheduled.SourceId),
                cancellationToken);

            if (await _scheduledNotificationRepository.TryMarkProcessedAsync(scheduled.Id, DateTime.UtcNow, cancellationToken))
            {
                processedCount++;
            }
        }

        return Result<int>.Success(processedCount);
    }
}
