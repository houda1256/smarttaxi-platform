using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Notifications.Events;

namespace SmartTaxi.Domain.Tests.Notifications.Entities;

public class NotificationTests
{
    private static Notification NewNotification(Guid? recipientId = null, Guid? sourceId = null) => Notification.Create(
        recipientId ?? Guid.NewGuid(), NotificationCategory.Subscription, "subscription.renewed", "Title", "Body",
        new Dictionary<string, string> { ["Key"] = "Value" }, isMandatory: false, sourceType: "Subscription",
        sourceId ?? Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public void Create_WithValidData_RaisesNotificationCreated()
    {
        var recipientId = Guid.NewGuid();
        var notification = NewNotification(recipientId);

        var raised = Assert.Single(notification.DomainEvents);
        var created = Assert.IsType<NotificationCreated>(raised);
        Assert.Equal(notification.Id, created.NotificationId);
        Assert.Equal(recipientId, created.RecipientUserId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            Guid.NewGuid(), NotificationCategory.System, "key", title, "body", new Dictionary<string, string>(),
            false, "Source", Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyRecipient_Throws()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(
            Guid.Empty, NotificationCategory.System, "key", "title", "body", new Dictionary<string, string>(),
            false, "Source", Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void ComputeIdempotencyKey_SameInputs_ProducesSameKey()
    {
        var sourceId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        var first = Notification.ComputeIdempotencyKey("Subscription", sourceId, recipientId, NotificationCategory.Subscription);
        var second = Notification.ComputeIdempotencyKey("Subscription", sourceId, recipientId, NotificationCategory.Subscription);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeIdempotencyKey_DifferentCategory_ProducesDifferentKey()
    {
        var sourceId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        var first = Notification.ComputeIdempotencyKey("Subscription", sourceId, recipientId, NotificationCategory.Subscription);
        var second = Notification.ComputeIdempotencyKey("Subscription", sourceId, recipientId, NotificationCategory.System);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void MarkAsRead_WhenUnread_SetsReadAtUtcAndRaisesEvent()
    {
        var notification = NewNotification();
        var utcNow = DateTime.UtcNow;

        notification.MarkAsRead(utcNow);

        Assert.True(notification.IsRead);
        Assert.Equal(utcNow, notification.ReadAtUtc);
        Assert.Contains(notification.DomainEvents, e => e is NotificationRead);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotentAndKeepsOriginalTimestamp()
    {
        var notification = NewNotification();
        var firstReadAt = DateTime.UtcNow;
        notification.MarkAsRead(firstReadAt);

        notification.MarkAsRead(firstReadAt.AddMinutes(5));

        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }
}
