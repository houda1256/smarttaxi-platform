using SmartTaxi.Application.Notifications.Queries.GetMyNotifications;
using SmartTaxi.Application.Notifications.Queries.GetMyUnreadNotificationCount;
using SmartTaxi.Application.Notifications.Queries.GetNotificationDeliveryFailures;
using SmartTaxi.Application.Notifications.Queries.GetNotificationTemplates;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.Notifications.Queries;

public class NotificationQueryHandlerTests
{
    private readonly FakeNotificationRepository _notificationRepository = new();
    private readonly FakeNotificationTemplateRepository _templateRepository = new();
    private readonly FakeNotificationDeliveryAttemptRepository _attemptRepository = new();

    private readonly GetMyNotificationsQueryHandler _getMineHandler;
    private readonly GetMyUnreadNotificationCountQueryHandler _getUnreadCountHandler;
    private readonly GetNotificationTemplatesQueryHandler _getTemplatesHandler;
    private readonly GetNotificationDeliveryFailuresQueryHandler _getFailuresHandler;

    public NotificationQueryHandlerTests()
    {
        _getMineHandler = new GetMyNotificationsQueryHandler(_notificationRepository);
        _getUnreadCountHandler = new GetMyUnreadNotificationCountQueryHandler(_notificationRepository);
        _getTemplatesHandler = new GetNotificationTemplatesQueryHandler(_templateRepository);
        _getFailuresHandler = new GetNotificationDeliveryFailuresQueryHandler(_attemptRepository);
    }

    private async Task<Notification> AddNotificationForAsync(Guid recipientUserId)
    {
        var notification = Notification.Create(
            recipientUserId, NotificationCategory.System, "system.test", "Title", "Body", new Dictionary<string, string>(),
            false, "Source", Guid.NewGuid(), DateTime.UtcNow);
        await _notificationRepository.TryAddAsync(notification, CancellationToken.None);
        return notification;
    }

    [Fact]
    public async Task GetMyNotifications_OnlyReturnsCallersOwnNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await AddNotificationForAsync(userId);
        await AddNotificationForAsync(otherUserId);

        var result = await _getMineHandler.Handle(new GetMyNotificationsQuery(userId, false, 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.All(result.Items, n => Assert.Equal(userId, n.RecipientUserId));
    }

    [Fact]
    public async Task GetMyNotifications_UnreadOnly_ExcludesReadNotifications()
    {
        var userId = Guid.NewGuid();
        var readOne = await AddNotificationForAsync(userId);
        await AddNotificationForAsync(userId);
        await _notificationRepository.TryMarkAsReadAsync(readOne.Id, userId, DateTime.UtcNow, CancellationToken.None);

        var result = await _getMineHandler.Handle(new GetMyNotificationsQuery(userId, true, 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.DoesNotContain(result.Items, n => n.Id == readOne.Id);
    }

    [Fact]
    public async Task GetMyUnreadCount_ReflectsOnlyUnreadForCaller()
    {
        var userId = Guid.NewGuid();
        await AddNotificationForAsync(userId);
        await AddNotificationForAsync(userId);

        var count = await _getUnreadCountHandler.Handle(new GetMyUnreadNotificationCountQuery(userId), CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetNotificationTemplates_ReturnsAllTemplates()
    {
        await _templateRepository.AddAsync(
            NotificationTemplate.Create(
                "key", NotificationCategory.System, NotificationChannel.Email, Language.French, "s", "b", DateTime.UtcNow),
            CancellationToken.None);

        var templates = await _getTemplatesHandler.Handle(new GetNotificationTemplatesQuery(), CancellationToken.None);

        Assert.Single(templates);
    }

    [Fact]
    public async Task GetDeliveryFailures_OnlyReturnsFailedAttempts()
    {
        var sentAttempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Email, 1, DateTime.UtcNow);
        sentAttempt.MarkSent(DateTime.UtcNow, null);
        await _attemptRepository.AddAsync(sentAttempt, CancellationToken.None);

        var failedAttempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Sms, 1, DateTime.UtcNow);
        failedAttempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", "sanitized", null);
        await _attemptRepository.AddAsync(failedAttempt, CancellationToken.None);

        var result = await _getFailuresHandler.Handle(new GetNotificationDeliveryFailuresQuery(1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(failedAttempt.Id, result.Items.Single().Id);
    }
}
