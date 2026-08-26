using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Commands.ProcessDueNotifications;
using SmartTaxi.Application.Notifications.Commands.ProcessRetryableDeliveries;
using SmartTaxi.Application.Notifications.Commands.ScheduleNotification;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.Notifications.Commands;

public class NotificationSchedulingCommandHandlerTests
{
    private readonly FakeScheduledNotificationRepository _scheduledRepository = new();
    private readonly FakeNotificationDeliveryAttemptRepository _attemptRepository = new();
    private readonly FakeNotificationRetryPolicy _retryPolicy = new();
    private readonly FakeNotificationDispatcher _dispatcher = new();

    private readonly ScheduleNotificationCommandHandler _scheduleHandler;
    private readonly ProcessDueNotificationsCommandHandler _processDueHandler;
    private readonly ProcessRetryableDeliveriesCommandHandler _processRetryableHandler;

    public NotificationSchedulingCommandHandlerTests()
    {
        _scheduleHandler = new ScheduleNotificationCommandHandler(_scheduledRepository);
        _processDueHandler = new ProcessDueNotificationsCommandHandler(_scheduledRepository, _dispatcher);
        _processRetryableHandler = new ProcessRetryableDeliveriesCommandHandler(_attemptRepository, _retryPolicy, _dispatcher);
    }

    [Fact]
    public async Task Schedule_ValidReminder_Succeeds()
    {
        var result = await _scheduleHandler.Handle(
            new ScheduleNotificationCommand(
                Guid.NewGuid(), NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
                false, "Subscription", Guid.NewGuid(), DateTime.UtcNow.AddDays(3)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    /// <summary>Built directly via the Domain factory (bypassing ScheduleNotificationCommandHandler) so it can be planted already "due" — Schedule's own guard only rejects a ScheduledAtUtc before the utcNow passed alongside it, and both are supplied here as a self-consistent past pair.</summary>
    private static ScheduledNotification NewDueScheduledNotification(Guid recipientId, Guid sourceId)
    {
        var pastBaseline = DateTime.UtcNow.AddDays(-1);
        return ScheduledNotification.Schedule(
            recipientId, NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            false, "Subscription", sourceId, pastBaseline.AddHours(1), pastBaseline);
    }

    [Fact]
    public async Task ProcessDueNotifications_DispatchesOnlyDueReminders_AndMarksThemProcessed()
    {
        var recipientId = Guid.NewGuid();
        var dueSourceId = Guid.NewGuid();
        var notDueSourceId = Guid.NewGuid();

        await _scheduledRepository.TryAddAsync(NewDueScheduledNotification(recipientId, dueSourceId), CancellationToken.None);

        await _scheduleHandler.Handle(
            new ScheduleNotificationCommand(
                recipientId, NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
                false, "Subscription", notDueSourceId, DateTime.UtcNow.AddDays(30)),
            CancellationToken.None);

        var result = await _processDueHandler.Handle(new ProcessDueNotificationsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Value);
        Assert.Single(_dispatcher.DispatchedRequests, request => request.SourceId == dueSourceId);
        Assert.DoesNotContain(_dispatcher.DispatchedRequests, request => request.SourceId == notDueSourceId);
    }

    [Fact]
    public async Task ProcessDueNotifications_CalledTwice_IsIdempotent()
    {
        var recipientId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        await _scheduledRepository.TryAddAsync(NewDueScheduledNotification(recipientId, sourceId), CancellationToken.None);

        var first = await _processDueHandler.Handle(new ProcessDueNotificationsCommand(), CancellationToken.None);
        var second = await _processDueHandler.Handle(new ProcessDueNotificationsCommand(), CancellationToken.None);

        Assert.Equal(1, first.Value);
        Assert.Equal(0, second.Value);
        Assert.Single(_dispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task ProcessRetryableDeliveries_RetriesEachEligibleAttempt()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Email, 1, DateTime.UtcNow.AddHours(-1));
        attempt.MarkFailed(DateTime.UtcNow.AddHours(-1), "SEND_FAILED", "sanitized", DateTime.UtcNow.AddMinutes(-1));
        await _attemptRepository.AddAsync(attempt, CancellationToken.None);

        var result = await _processRetryableHandler.Handle(new ProcessRetryableDeliveriesCommand(), CancellationToken.None);

        Assert.Equal(1, result.Value);
        Assert.Contains(attempt.Id, _dispatcher.RetriedDeliveryAttemptIds);
    }

    [Fact]
    public async Task ProcessRetryableDeliveries_AttemptNotYetDue_IsNotRetried()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Email, 1, DateTime.UtcNow);
        attempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", "sanitized", DateTime.UtcNow.AddHours(1));
        await _attemptRepository.AddAsync(attempt, CancellationToken.None);

        var result = await _processRetryableHandler.Handle(new ProcessRetryableDeliveriesCommand(), CancellationToken.None);

        Assert.Equal(0, result.Value);
    }
}
