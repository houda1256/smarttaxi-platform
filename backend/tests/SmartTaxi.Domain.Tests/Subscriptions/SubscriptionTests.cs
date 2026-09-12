using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Domain.Subscriptions.Events;

namespace SmartTaxi.Domain.Tests.Subscriptions;

public class SubscriptionTests
{
    [Fact]
    public void CreatePending_WithValidDates_SetsStatusPendingAndRaisesEvent()
    {
        var subscriberId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);

        var subscription = Subscription.CreatePending(subscriberId, planId, UserRole.Driver, start, end, true, null, start);

        Assert.Equal(SubscriptionStatus.Pending, subscription.Status);
        Assert.Equal(subscriberId, subscription.SubscriberId);
        Assert.Equal(planId, subscription.PlanId);
        var raised = Assert.Single(subscription.DomainEvents);
        var created = Assert.IsType<SubscriptionCreated>(raised);
        Assert.Equal(subscription.Id, created.SubscriptionId);
    }

    [Fact]
    public void CreatePending_WithEndDateNotAfterStartDate_Throws()
    {
        var start = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => Subscription.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), UserRole.Driver, start, start, false, null, start));
    }

    [Fact]
    public void IsEffectivelyActive_WhenActiveAndBeforeEndDate_ReturnsTrue()
    {
        var start = DateTime.UtcNow;
        var subscription = Subscription.CreatePending(Guid.NewGuid(), Guid.NewGuid(), UserRole.Driver, start, start.AddMonths(1), false, null, start);
        typeof(Subscription).GetProperty(nameof(Subscription.Status))!.SetValue(subscription, SubscriptionStatus.Active);

        Assert.True(subscription.IsEffectivelyActive(start.AddDays(1)));
    }

    [Fact]
    public void IsEffectivelyActive_PastEndDate_ReturnsFalseEvenIfStatusStillActive()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);
        var subscription = Subscription.CreatePending(Guid.NewGuid(), Guid.NewGuid(), UserRole.Driver, start, end, false, null, start);
        typeof(Subscription).GetProperty(nameof(Subscription.Status))!.SetValue(subscription, SubscriptionStatus.Active);

        Assert.False(subscription.IsEffectivelyActive(end.AddSeconds(1)));
    }

    [Fact]
    public void IsEffectivelyActive_WhenPending_ReturnsFalse()
    {
        var start = DateTime.UtcNow;
        var subscription = Subscription.CreatePending(Guid.NewGuid(), Guid.NewGuid(), UserRole.Driver, start, start.AddMonths(1), false, null, start);

        Assert.False(subscription.IsEffectivelyActive(start));
    }
}
