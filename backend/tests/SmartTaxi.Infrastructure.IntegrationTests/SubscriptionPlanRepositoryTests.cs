using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Infrastructure.Subscriptions.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the unique index on Code and the round-trip of Features/Limits (stored as delimited text / JSON) against a real PostgreSQL database.</summary>
[Collection("SharedPostgres")]
public class SubscriptionPlanRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public SubscriptionPlanRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static SubscriptionPlan NewPlan(string code) => SubscriptionPlan.Create(
        "Driver Pro", code, "desc", UserRole.Driver, 29.90m, "TND", BillingPeriod.Monthly, 7,
        ["PriorityDispatch", "AdvancedAnalytics"], new Dictionary<string, int> { ["VehiclesCount"] = 3, ["DriversCount"] = 5 },
        DateTime.UtcNow);

    [Fact]
    public async Task AddAndGetById_RoundTripsFeaturesAndLimits()
    {
        var plan = NewPlan($"DRIVER_PRO_{Guid.NewGuid():N}");

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionPlanRepository(writeContext).TryAddAsync(plan, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionPlanRepository(readContext).GetByIdAsync(plan.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded!.Features.Count);
        Assert.Contains("AdvancedAnalytics", reloaded.Features);
        Assert.Equal(3, reloaded.Limits["VehiclesCount"]);
        Assert.Equal(5, reloaded.Limits["DriversCount"]);
    }

    [Fact]
    public async Task TryAddAsync_DuplicateCode_Fails()
    {
        var code = $"DUP_{Guid.NewGuid():N}";
        var first = NewPlan(code);
        var second = NewPlan(code);

        await using var context1 = _fixture.CreateContext();
        Assert.True(await new SubscriptionPlanRepository(context1).TryAddAsync(first, CancellationToken.None));

        await using var context2 = _fixture.CreateContext();
        Assert.False(await new SubscriptionPlanRepository(context2).TryAddAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task TryDeactivateThenTryActivate_RoundTrips()
    {
        var plan = NewPlan($"CODE_{Guid.NewGuid():N}");

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionPlanRepository(writeContext).TryAddAsync(plan, CancellationToken.None);
        }

        await using (var deactivateContext = _fixture.CreateContext())
        {
            Assert.True(await new SubscriptionPlanRepository(deactivateContext).TryDeactivateAsync(plan.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using (var secondDeactivateContext = _fixture.CreateContext())
        {
            Assert.False(await new SubscriptionPlanRepository(secondDeactivateContext).TryDeactivateAsync(plan.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using var activateContext = _fixture.CreateContext();
        Assert.True(await new SubscriptionPlanRepository(activateContext).TryActivateAsync(plan.Id, DateTime.UtcNow, CancellationToken.None));
    }
}
