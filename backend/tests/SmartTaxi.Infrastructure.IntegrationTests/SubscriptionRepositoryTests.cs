using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Infrastructure.Subscriptions.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the partial unique index on (SubscriberId, TargetRole) and the atomic status guards against a real PostgreSQL database.</summary>
[Collection("SharedPostgres")]
public class SubscriptionRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public SubscriptionRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Subscription NewSubscription(Guid subscriberId, Guid planId, UserRole role = UserRole.Driver) =>
        Subscription.CreatePending(subscriberId, planId, role, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), false, null, DateTime.UtcNow);

    [Fact]
    public async Task TryAddAsync_SecondPendingOrActiveForSameSubscriberAndRole_Fails()
    {
        var subscriberId = Guid.NewGuid();
        var first = NewSubscription(subscriberId, Guid.NewGuid());
        var second = NewSubscription(subscriberId, Guid.NewGuid());

        await using var context = _fixture.CreateContext();
        var repository = new SubscriptionRepository(context);

        Assert.True(await repository.TryAddAsync(first, CancellationToken.None));

        await using var context2 = _fixture.CreateContext();
        var repository2 = new SubscriptionRepository(context2);
        Assert.False(await repository2.TryAddAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task TryAddAsync_SameSubscriberDifferentRole_Succeeds()
    {
        var subscriberId = Guid.NewGuid();
        var driverSubscription = NewSubscription(subscriberId, Guid.NewGuid(), UserRole.Driver);
        var ownerSubscription = NewSubscription(subscriberId, Guid.NewGuid(), UserRole.TaxiOwner);

        await using var context = _fixture.CreateContext();
        Assert.True(await new SubscriptionRepository(context).TryAddAsync(driverSubscription, CancellationToken.None));

        await using var context2 = _fixture.CreateContext();
        Assert.True(await new SubscriptionRepository(context2).TryAddAsync(ownerSubscription, CancellationToken.None));
    }

    [Fact]
    public async Task TryAddAsync_AfterCancellingFirst_AllowsANewOneForSameRole()
    {
        var subscriberId = Guid.NewGuid();
        var first = NewSubscription(subscriberId, Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionRepository(writeContext).TryAddAsync(first, CancellationToken.None);
        }

        await using (var cancelContext = _fixture.CreateContext())
        {
            await new SubscriptionRepository(cancelContext).TryCancelAsync(first.Id, DateTime.UtcNow, CancellationToken.None);
        }

        var second = NewSubscription(subscriberId, Guid.NewGuid());
        await using var context2 = _fixture.CreateContext();
        Assert.True(await new SubscriptionRepository(context2).TryAddAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task TryActivateAsync_FromPending_Succeeds()
    {
        var subscription = NewSubscription(Guid.NewGuid(), Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionRepository(writeContext).TryAddAsync(subscription, CancellationToken.None);
        }

        await using var activateContext = _fixture.CreateContext();
        var activated = await new SubscriptionRepository(activateContext).TryActivateAsync(subscription.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.True(activated);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionRepository(readContext).GetByIdAsync(subscription.Id, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, reloaded!.Status);
    }

    [Fact]
    public async Task TryCancelAsync_TwoConcurrentCancels_OnlyOneSucceeds()
    {
        var subscription = NewSubscription(Guid.NewGuid(), Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new SubscriptionRepository(writeContext);
            await repo.TryAddAsync(subscription, CancellationToken.None);
            await repo.TryActivateAsync(subscription.Id, DateTime.UtcNow, CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new SubscriptionRepository(context1).TryCancelAsync(subscription.Id, DateTime.UtcNow, CancellationToken.None),
            new SubscriptionRepository(context2).TryCancelAsync(subscription.Id, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
    }

    [Fact]
    public async Task TryRenewAsync_TwoConcurrentRenewalsWithSameExpectedEndDate_OnlyOneSucceedsAndEndDateReflectsExactlyOnePeriod()
    {
        var subscription = NewSubscription(Guid.NewGuid(), Guid.NewGuid());
        var originalEndDate = subscription.EndDate;
        var firstAttemptEndDate = originalEndDate.AddMonths(1);
        var secondAttemptEndDate = originalEndDate.AddMonths(2);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repo = new SubscriptionRepository(writeContext);
            await repo.TryAddAsync(subscription, CancellationToken.None);
            await repo.TryActivateAsync(subscription.Id, DateTime.UtcNow, CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        // Both requests read the same originalEndDate (simulating two concurrent renewals racing on
        // the same subscription) and race their compare-and-swap reservation against it.
        var results = await Task.WhenAll(
            new SubscriptionRepository(context1).TryRenewAsync(
                subscription.Id, originalEndDate, firstAttemptEndDate, DateTime.UtcNow, CancellationToken.None),
            new SubscriptionRepository(context2).TryRenewAsync(
                subscription.Id, originalEndDate, secondAttemptEndDate, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionRepository(readContext).GetByIdAsync(subscription.Id, CancellationToken.None);

        // Exactly one renewal period was applied — never both (lost update) and never neither.
        Assert.True(reloaded!.EndDate == firstAttemptEndDate || reloaded.EndDate == secondAttemptEndDate);
    }
}
