using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Tests.Notifications.Entities;

public class ScheduledNotificationTests
{
    private static ScheduledNotification NewScheduled(DateTime? scheduledAtUtc = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return ScheduledNotification.Schedule(
            Guid.NewGuid(), NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            isMandatory: false, sourceType: "Subscription", Guid.NewGuid(), scheduledAtUtc ?? now.AddDays(3), now);
    }

    [Fact]
    public void Schedule_WithValidData_SetsStatusPending()
    {
        var scheduled = NewScheduled();

        Assert.Equal(ScheduledNotificationStatus.Pending, scheduled.Status);
        Assert.Null(scheduled.ProcessedAtUtc);
    }

    [Fact]
    public void Schedule_WithPastScheduledDate_Throws()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => ScheduledNotification.Schedule(
            Guid.NewGuid(), NotificationCategory.System, "key", new Dictionary<string, string>(), false, "Source",
            Guid.NewGuid(), now.AddDays(-1), now));
    }

    [Fact]
    public void MarkProcessed_SetsStatusAndTimestamp()
    {
        var scheduled = NewScheduled();
        var utcNow = DateTime.UtcNow;

        scheduled.MarkProcessed(utcNow);

        Assert.Equal(ScheduledNotificationStatus.Processed, scheduled.Status);
        Assert.Equal(utcNow, scheduled.ProcessedAtUtc);
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var scheduled = NewScheduled();

        scheduled.Cancel(DateTime.UtcNow);

        Assert.Equal(ScheduledNotificationStatus.Cancelled, scheduled.Status);
    }

    [Fact]
    public void MarkFailed_SetsStatusFailed()
    {
        var scheduled = NewScheduled();

        scheduled.MarkFailed(DateTime.UtcNow);

        Assert.Equal(ScheduledNotificationStatus.Failed, scheduled.Status);
    }
}
