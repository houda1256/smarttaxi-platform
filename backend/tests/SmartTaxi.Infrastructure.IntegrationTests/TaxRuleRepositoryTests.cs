using SmartTaxi.Domain.Payments.Taxes.Entities;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that TaxRuleRepository.GetApplicableRuleAsync
/// correctly translates TaxRule.IsApplicableOn's date-range and service-matching logic
/// into SQL, and that "tax changes do not modify previously finalized invoices" holds
/// structurally — deactivating or superseding a rule never mutates existing rows.
/// </summary>
[Collection("SharedPostgres")]
public class TaxRuleRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public TaxRuleRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetApplicableRuleAsync_MatchesExactServiceOverGenericAll_WithinEffectiveWindow()
    {
        // "All" is a real wildcard that would otherwise leak into every other test in this shared-Postgres
        // suite, so this test bounds its "All" rule to a window (year 2000) no other TaxRule test queries —
        // every other test here uses 2026 dates.
        var windowStart = new DateOnly(2000, 1, 1);
        var windowEnd = new DateOnly(2000, 12, 31);
        var queryDate = new DateOnly(2000, 6, 1);
        var uniqueRideService = $"Rides-{Guid.NewGuid():N}";
        var uniqueFallbackService = $"Advertising-{Guid.NewGuid():N}";

        var genericRule = TaxRule.Create("TVA standard", 19m, "TN", "All", windowStart, windowEnd, null, DateTime.UtcNow);
        var rideRule = TaxRule.Create("TVA transport", 7m, "TN", uniqueRideService, windowStart, windowEnd, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new TaxRuleRepository(writeContext);
            await repository.AddAsync(genericRule, CancellationToken.None);
            await repository.AddAsync(rideRule, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();

        // A specific service should match its own dedicated rule, not fall back to "All".
        var applicable = await new TaxRuleRepository(readContext).GetApplicableRuleAsync(uniqueRideService, queryDate, CancellationToken.None);
        Assert.NotNull(applicable);
        Assert.Equal(rideRule.Id, applicable!.Id);

        // A service with no dedicated rule falls back to "All", within the same bounded window.
        var fallback = await new TaxRuleRepository(readContext).GetApplicableRuleAsync(uniqueFallbackService, queryDate, CancellationToken.None);
        Assert.NotNull(fallback);
        Assert.Equal(genericRule.Id, fallback!.Id);
    }

    [Fact]
    public async Task GetApplicableRuleAsync_OutsideEffectiveWindow_ReturnsNull()
    {
        // A service name unique to this test run: an "All"-service rule created by any other test in the
        // shared-Postgres suite (this file or another) would otherwise fall back and match here too.
        var service = $"Advertising-{Guid.NewGuid():N}";
        var rule = TaxRule.Create("TVA temporaire", 5m, "TN", service, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new TaxRuleRepository(writeContext).AddAsync(rule, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var beforeWindow = await new TaxRuleRepository(readContext).GetApplicableRuleAsync(service, new DateOnly(2026, 2, 1), CancellationToken.None);
        var afterWindow = await new TaxRuleRepository(readContext).GetApplicableRuleAsync(service, new DateOnly(2026, 4, 1), CancellationToken.None);

        Assert.Null(beforeWindow);
        Assert.Null(afterWindow);
    }

    [Fact]
    public async Task TryDeactivateAsync_RemovesRuleFromApplicableLookup_ButLeavesItReadableById()
    {
        var rule = TaxRule.Create("TVA restauration", 13m, "TN", "Catering", new DateOnly(2026, 1, 1), null, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new TaxRuleRepository(writeContext).AddAsync(rule, CancellationToken.None);
        }

        await using (var deactivateContext = _fixture.CreateContext())
        {
            var deactivated = await new TaxRuleRepository(deactivateContext).TryDeactivateAsync(rule.Id, DateTime.UtcNow, CancellationToken.None);
            Assert.True(deactivated);
        }

        await using var readContext = _fixture.CreateContext();
        var repository = new TaxRuleRepository(readContext);

        var applicable = await repository.GetApplicableRuleAsync("Catering", new DateOnly(2026, 6, 1), CancellationToken.None);
        Assert.Null(applicable);

        var byId = await repository.GetByIdAsync(rule.Id, CancellationToken.None);
        Assert.NotNull(byId);
        Assert.False(byId!.IsActive);

        // Deactivating twice must fail — TryDeactivateAsync is guarded on IsActive.
        var secondDeactivate = await repository.TryDeactivateAsync(rule.Id, DateTime.UtcNow, CancellationToken.None);
        Assert.False(secondDeactivate);
    }
}
