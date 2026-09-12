using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Infrastructure.Notifications.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the IdempotencyKey unique index against a real PostgreSQL database — the mechanism that stops a retried business event from creating a duplicate notification for the same recipient (spec section 13).</summary>
[Collection("SharedPostgres")]
public class NotificationRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public NotificationRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Notification NewNotification(Guid recipientId, string sourceType, Guid sourceId, NotificationCategory category = NotificationCategory.Subscription) =>
        Notification.Create(
            recipientId, category, "subscription.renewed", "Title", "Body", new Dictionary<string, string> { ["K"] = "V" },
            false, sourceType, sourceId, DateTime.UtcNow);

    [Fact]
    public async Task TryAddAsync_ThenGetById_RoundTripsAllFields()
    {
        var notification = NewNotification(Guid.NewGuid(), "Subscription", Guid.NewGuid());

        await using var context = _fixture.CreateContext();
        var repository = new NotificationRepository(context);
        await repository.TryAddAsync(notification, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new NotificationRepository(readContext).GetByIdAsync(notification.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(notification.Title, reloaded!.Title);
        Assert.Equal(notification.Body, reloaded.Body);
        Assert.Equal("V", reloaded.Variables["K"]);
        Assert.False(reloaded.IsRead);
    }

    [Fact]
    public async Task TryAddAsync_TwoConcurrentDispatchesForSameIdempotencyKey_OnlyOneSucceeds()
    {
        var recipientId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var first = NewNotification(recipientId, "Subscription", sourceId);
        var second = NewNotification(recipientId, "Subscription", sourceId);

        Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new NotificationRepository(context1).TryAddAsync(first, CancellationToken.None),
            new NotificationRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await new NotificationRepository(readContext).GetUnreadCountAsync(recipientId, CancellationToken.None);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetUnreadCountAsync_OnlyCountsUnreadForThatRecipient()
    {
        var recipientId = Guid.NewGuid();
        var readOne = NewNotification(recipientId, "Ride", Guid.NewGuid());
        var unreadOne = NewNotification(recipientId, "Ride", Guid.NewGuid());
        var otherUsers = NewNotification(Guid.NewGuid(), "Ride", Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new NotificationRepository(writeContext);
            await repo.TryAddAsync(readOne, CancellationToken.None);
            await repo.TryAddAsync(unreadOne, CancellationToken.None);
            await repo.TryAddAsync(otherUsers, CancellationToken.None);
            await repo.TryMarkAsReadAsync(readOne.Id, recipientId, DateTime.UtcNow, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var count = await new NotificationRepository(readContext).GetUnreadCountAsync(recipientId, CancellationToken.None);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TryMarkAsReadAsync_ByAnotherUser_Fails()
    {
        var ownerId = Guid.NewGuid();
        var notification = NewNotification(ownerId, "Ride", Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new NotificationRepository(writeContext).TryAddAsync(notification, CancellationToken.None);
        }

        await using var context = _fixture.CreateContext();
        var succeeded = await new NotificationRepository(context)
            .TryMarkAsReadAsync(notification.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        Assert.False(succeeded);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_TwoConcurrentCalls_NeverDoubleCountsAndLeavesAllRead()
    {
        var recipientId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new NotificationRepository(writeContext);
            await repo.TryAddAsync(NewNotification(recipientId, "Ride", Guid.NewGuid()), CancellationToken.None);
            await repo.TryAddAsync(NewNotification(recipientId, "Ride", Guid.NewGuid()), CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new NotificationRepository(context1).MarkAllAsReadAsync(recipientId, DateTime.UtcNow, CancellationToken.None),
            new NotificationRepository(context2).MarkAllAsReadAsync(recipientId, DateTime.UtcNow, CancellationToken.None));

        Assert.Equal(2, results.Sum());

        await using var readContext = _fixture.CreateContext();
        var remainingUnread = await new NotificationRepository(readContext).GetUnreadCountAsync(recipientId, CancellationToken.None);
        Assert.Equal(0, remainingUnread);
    }
}
