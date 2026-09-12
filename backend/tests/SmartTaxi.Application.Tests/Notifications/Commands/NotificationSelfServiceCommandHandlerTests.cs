using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Commands.MarkAllNotificationsRead;
using SmartTaxi.Application.Notifications.Commands.MarkNotificationRead;
using SmartTaxi.Application.Notifications.Commands.RegisterDeviceToken;
using SmartTaxi.Application.Notifications.Commands.RevokeDeviceToken;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.Notifications.Commands;

/// <summary>Security-focused: every self-service mutation must be scoped to the caller's own resources — see spec section 23.</summary>
public class NotificationSelfServiceCommandHandlerTests
{
    private readonly FakeNotificationRepository _notificationRepository = new();
    private readonly FakeDeviceTokenRepository _deviceTokenRepository = new();

    private readonly MarkNotificationReadCommandHandler _markReadHandler;
    private readonly MarkAllNotificationsReadCommandHandler _markAllReadHandler;
    private readonly RegisterDeviceTokenCommandHandler _registerDeviceTokenHandler;
    private readonly RevokeDeviceTokenCommandHandler _revokeDeviceTokenHandler;

    public NotificationSelfServiceCommandHandlerTests()
    {
        _markReadHandler = new MarkNotificationReadCommandHandler(_notificationRepository);
        _markAllReadHandler = new MarkAllNotificationsReadCommandHandler(_notificationRepository);
        _registerDeviceTokenHandler = new RegisterDeviceTokenCommandHandler(_deviceTokenRepository);
        _revokeDeviceTokenHandler = new RevokeDeviceTokenCommandHandler(_deviceTokenRepository);
    }

    private async Task<Notification> CreateNotificationForAsync(Guid recipientUserId)
    {
        var notification = Notification.Create(
            recipientUserId, NotificationCategory.System, "system.test", "Title", "Body", new Dictionary<string, string>(),
            false, "Source", Guid.NewGuid(), DateTime.UtcNow);
        await _notificationRepository.TryAddAsync(notification, CancellationToken.None);
        return notification;
    }

    [Fact]
    public async Task MarkRead_ByOwner_Succeeds()
    {
        var userId = Guid.NewGuid();
        var notification = await CreateNotificationForAsync(userId);

        var result = await _markReadHandler.Handle(new MarkNotificationReadCommand(notification.Id, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _notificationRepository.GetByIdAsync(notification.Id, CancellationToken.None);
        Assert.True(reloaded!.IsRead);
    }

    [Fact]
    public async Task MarkRead_ByAnotherUser_ReturnsForbiddenAndLeavesUnread()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var notification = await CreateNotificationForAsync(ownerId);

        var result = await _markReadHandler.Handle(new MarkNotificationReadCommand(notification.Id, attackerId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _notificationRepository.GetByIdAsync(notification.Id, CancellationToken.None);
        Assert.False(reloaded!.IsRead);
    }

    [Fact]
    public async Task MarkRead_UnknownNotification_ReturnsNotFound()
    {
        var result = await _markReadHandler.Handle(new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task MarkAllRead_OnlyAffectsCallersOwnNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await CreateNotificationForAsync(userId);
        await CreateNotificationForAsync(userId);
        var otherUsersNotification = await CreateNotificationForAsync(otherUserId);

        var result = await _markAllReadHandler.Handle(new MarkAllNotificationsReadCommand(userId), CancellationToken.None);

        Assert.Equal(2, result.Value);
        var otherReloaded = await _notificationRepository.GetByIdAsync(otherUsersNotification.Id, CancellationToken.None);
        Assert.False(otherReloaded!.IsRead);
    }

    [Fact]
    public async Task RegisterDeviceToken_ForSelf_Succeeds()
    {
        var userId = Guid.NewGuid();

        var result = await _registerDeviceTokenHandler.Handle(
            new RegisterDeviceTokenCommand(userId, "raw-token-abc", "android"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var token = await _deviceTokenRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(userId, token!.UserId);
    }

    [Fact]
    public async Task RegisterDeviceToken_AlreadyOwnedByAnotherUser_ReassignsToNewCaller()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var firstResult = await _registerDeviceTokenHandler.Handle(
            new RegisterDeviceTokenCommand(firstUserId, "shared-token", "ios"), CancellationToken.None);

        var secondResult = await _registerDeviceTokenHandler.Handle(
            new RegisterDeviceTokenCommand(secondUserId, "shared-token", "ios"), CancellationToken.None);

        Assert.True(secondResult.IsSuccess);
        Assert.Equal(firstResult.Value, secondResult.Value);
        var token = await _deviceTokenRepository.GetByIdAsync(secondResult.Value, CancellationToken.None);
        Assert.Equal(secondUserId, token!.UserId);
    }

    [Fact]
    public async Task RevokeDeviceToken_ByOwner_Succeeds()
    {
        var userId = Guid.NewGuid();
        var registered = await _registerDeviceTokenHandler.Handle(
            new RegisterDeviceTokenCommand(userId, "raw-token-xyz", "web"), CancellationToken.None);

        var result = await _revokeDeviceTokenHandler.Handle(new RevokeDeviceTokenCommand(registered.Value, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var token = await _deviceTokenRepository.GetByIdAsync(registered.Value, CancellationToken.None);
        Assert.False(token!.IsActive);
    }

    [Fact]
    public async Task RevokeDeviceToken_ByAnotherUser_ReturnsForbiddenAndLeavesTokenActive()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var registered = await _registerDeviceTokenHandler.Handle(
            new RegisterDeviceTokenCommand(ownerId, "raw-token-secure", "ios"), CancellationToken.None);

        var result = await _revokeDeviceTokenHandler.Handle(new RevokeDeviceTokenCommand(registered.Value, attackerId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var token = await _deviceTokenRepository.GetByIdAsync(registered.Value, CancellationToken.None);
        Assert.True(token!.IsActive);
    }
}
