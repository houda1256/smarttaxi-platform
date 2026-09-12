using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Infrastructure.Notifications.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the due-sweep query and the atomic TryMarkProcessedAsync guard against a real PostgreSQL database.</summary>
[Collection("SharedPostgres")]
public class ScheduledNotificationRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public ScheduledNotificationRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static ScheduledNotification NewDue(Guid sourceId)
    {
        var pastBaseline = DateTime.UtcNow.AddDays(-1);
        return ScheduledNotification.Schedule(
            Guid.NewGuid(), NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            false, "Subscription", sourceId, pastBaseline.AddHours(1), pastBaseline);
    }

    [Fact]
    public async Task GetDueAsync_OnlyReturnsPendingAndScheduledInThePast()
    {
        var due = NewDue(Guid.NewGuid());
        var notDue = ScheduledNotification.Schedule(
            Guid.NewGuid(), NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            false, "Subscription", Guid.NewGuid(), DateTime.UtcNow.AddDays(30), DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new ScheduledNotificationRepository(writeContext);
            await repo.TryAddAsync(due, CancellationToken.None);
            await repo.TryAddAsync(notDue, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var dueItems = await new ScheduledNotificationRepository(readContext).GetDueAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Contains(dueItems, s => s.Id == due.Id);
        Assert.DoesNotContain(dueItems, s => s.Id == notDue.Id);
    }

    [Fact]
    public async Task TryAddAsync_TwoScheduledForSameIdempotencyKey_OnlyOneSucceeds()
    {
        var sourceId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var first = ScheduledNotification.Schedule(
            recipientId, NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            false, "Subscription", sourceId, now.AddDays(3), now);
        var second = ScheduledNotification.Schedule(
            recipientId, NotificationCategory.Subscription, "subscription.expiring-soon", new Dictionary<string, string>(),
            false, "Subscription", sourceId, now.AddDays(3), now);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new ScheduledNotificationRepository(context1).TryAddAsync(first, CancellationToken.None),
            new ScheduledNotificationRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }

    [Fact]
    public async Task TryMarkProcessedAsync_TwoConcurrentSweepsOfSameRow_OnlyOneSucceeds()
    {
        var due = NewDue(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new ScheduledNotificationRepository(writeContext).TryAddAsync(due, CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new ScheduledNotificationRepository(context1).TryMarkProcessedAsync(due.Id, DateTime.UtcNow, CancellationToken.None),
            new ScheduledNotificationRepository(context2).TryMarkProcessedAsync(due.Id, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }
}
