using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Tests.Notifications.Entities;

public class NotificationDeliveryAttemptTests
{
    [Fact]
    public void Create_SetsStatusPending()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Email, 1, DateTime.UtcNow);

        Assert.Equal(NotificationDeliveryStatus.Pending, attempt.Status);
        Assert.Equal(1, attempt.AttemptNumber);
    }

    [Fact]
    public void Create_WithNonPositiveAttemptNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Email, 0, DateTime.UtcNow));
    }

    [Fact]
    public void MarkSent_SetsStatusAndSentAtUtc()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Push, 1, DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        attempt.MarkSent(utcNow, "provider-123");

        Assert.Equal(NotificationDeliveryStatus.Sent, attempt.Status);
        Assert.Equal(utcNow, attempt.SentAtUtc);
        Assert.Equal("provider-123", attempt.ProviderMessageId);
    }

    [Fact]
    public void MarkDelivered_SetsStatusAndDeliveredAtUtc()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Push, 1, DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;

        attempt.MarkDelivered(utcNow);

        Assert.Equal(NotificationDeliveryStatus.Delivered, attempt.Status);
        Assert.Equal(utcNow, attempt.DeliveredAtUtc);
    }

    [Fact]
    public void MarkFailed_SetsFailureDetailAndNextAttempt()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Sms, 1, DateTime.UtcNow);
        var nextAttempt = DateTime.UtcNow.AddMinutes(15);

        attempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", "sanitized", nextAttempt);

        Assert.Equal(NotificationDeliveryStatus.Failed, attempt.Status);
        Assert.Equal("SEND_FAILED", attempt.FailureCode);
        Assert.Equal(nextAttempt, attempt.NextAttemptAtUtc);
    }

    [Fact]
    public void MarkFailed_WithoutFailureCode_Throws()
    {
        var attempt = NotificationDeliveryAttempt.Create(Guid.NewGuid(), NotificationChannel.Sms, 1, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => attempt.MarkFailed(DateTime.UtcNow, "", "message", null));
    }
}
