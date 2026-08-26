using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;
using SmartTaxi.Infrastructure.Identity.Services;
using SmartTaxi.Infrastructure.Notifications;
using SmartTaxi.Infrastructure.Notifications.Options;
using SmartTaxi.Infrastructure.Notifications.Policies;
using SmartTaxi.Infrastructure.Notifications.Repositories;
using SmartTaxi.Infrastructure.Notifications.Services;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// End-to-end proof (real PostgreSQL, real policies/renderer/senders — only the
/// SignalR realtime leg is stubbed, since it lives in the API project) that
/// INotificationDispatcher is idempotent under real concurrency: two callers
/// racing to notify about the same business event never create two
/// Notification rows for the same recipient. See spec section 13.
/// </summary>
[Collection("SharedPostgres")]
public class NotificationDispatcherIntegrationTests
{
    private sealed class NoOpNotificationRealtimeNotifier : INotificationRealtimeNotifier
    {
        public Task NotifyNewNotificationAsync(Guid recipientUserId, Guid notificationId, string category, string title, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task NotifyUnreadCountChangedAsync(Guid recipientUserId, int unreadCount, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private readonly SharedPostgresFixture _fixture;

    public NotificationDispatcherIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static NotificationDispatcher CreateDispatcher(ApplicationDbContext context, NotificationOptions? options = null)
    {
        var notificationOptions = Options.Create(options ?? new NotificationOptions());

        return new NotificationDispatcher(
            new NotificationRepository(context),
            new NotificationDeliveryAttemptRepository(context),
            new NotificationPreferencePolicy(new UserPreferencesRepository(context), notificationOptions),
            new Application.Notifications.NotificationTemplateRenderer(new NotificationTemplateRepository(context)),
            new NotificationRetryPolicy(notificationOptions),
            new UserRepository(context),
            new UserPreferencesRepository(context),
            new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance),
            new LoggingSmsSender(NullLogger<LoggingSmsSender>.Instance),
            new LoggingPushNotificationSender(NullLogger<LoggingPushNotificationSender>.Instance),
            new DeviceTokenRepository(context),
            new NoOpNotificationRealtimeNotifier(),
            NullLogger<NotificationDispatcher>.Instance);
    }

    private async Task<Guid> CreateUserAsync(ApplicationDbContext context)
    {
        var email = Email.Create($"user-{Guid.NewGuid():N}@example.com");
        var user = User.Create(email, HashedPassword.Create("hash"), UserRole.Customer);
        await new UserRepository(context).AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task DispatchAsync_TwoConcurrentDispatchesForSameBusinessEvent_OnlyOneNotificationIsCreated()
    {
        Guid userId;
        await using (var setupContext = _fixture.CreateContext())
        {
            userId = await CreateUserAsync(setupContext);
        }

        var sourceId = Guid.NewGuid();
        var request = new NotificationRequest(
            userId, NotificationCategory.Subscription, "subscription.renewed", new Dictionary<string, string> { ["Plan"] = "Pro" },
            IsMandatory: false, SourceType: "Subscription", SourceId: sourceId);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        await Task.WhenAll(
            CreateDispatcher(context1).DispatchAsync(request, CancellationToken.None),
            CreateDispatcher(context2).DispatchAsync(request, CancellationToken.None));

        await using var readContext = _fixture.CreateContext();
        var unreadCount = await new NotificationRepository(readContext).GetUnreadCountAsync(userId, CancellationToken.None);

        Assert.Equal(1, unreadCount);
    }

    [Fact]
    public async Task DispatchAsync_Mandatory_IsDeliveredEvenWhenRecipientOptedOutOfAllChannels()
    {
        Guid userId;
        await using (var setupContext = _fixture.CreateContext())
        {
            userId = await CreateUserAsync(setupContext);
            var preferences = UserPreferences.CreateDefault(userId, DateTime.UtcNow);
            preferences.Update(Language.French, NotificationChannel.None, false, false, "UTC", null, null, DateTime.UtcNow);
            await new UserPreferencesRepository(setupContext).AddAsync(preferences, CancellationToken.None);
        }

        var request = new NotificationRequest(
            userId, NotificationCategory.Payment, "payment.confirmed", new Dictionary<string, string>(),
            IsMandatory: true, SourceType: "Payment", SourceId: Guid.NewGuid());

        await using var context = _fixture.CreateContext();
        await CreateDispatcher(context).DispatchAsync(request, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var unreadCount = await new NotificationRepository(readContext).GetUnreadCountAsync(userId, CancellationToken.None);

        Assert.Equal(1, unreadCount);
    }

    [Fact]
    public async Task DispatchAsync_Optional_WhenRecipientOptedOutOfAllChannels_CreatesNoNotification()
    {
        Guid userId;
        await using (var setupContext = _fixture.CreateContext())
        {
            userId = await CreateUserAsync(setupContext);
            var preferences = UserPreferences.CreateDefault(userId, DateTime.UtcNow);
            preferences.Update(Language.French, NotificationChannel.None, false, false, "UTC", null, null, DateTime.UtcNow);
            await new UserPreferencesRepository(setupContext).AddAsync(preferences, CancellationToken.None);
        }

        var request = new NotificationRequest(
            userId, NotificationCategory.Subscription, "subscription.renewed", new Dictionary<string, string>(),
            IsMandatory: false, SourceType: "Subscription", SourceId: Guid.NewGuid());

        await using var context = _fixture.CreateContext();
        await CreateDispatcher(context).DispatchAsync(request, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var unreadCount = await new NotificationRepository(readContext).GetUnreadCountAsync(userId, CancellationToken.None);

        Assert.Equal(0, unreadCount);
    }
}
