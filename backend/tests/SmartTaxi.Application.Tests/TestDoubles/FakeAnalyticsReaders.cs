using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdminDashboardReader : IAdminDashboardReader
{
    public AdminDashboardSummary Summary { get; set; } =
        new(0, 0, 0, 0, 0, 0, 0, 0m, 0m, 0, 0, 0, 0, 0, 0);

    public int CallCount { get; private set; }

    public Task<AdminDashboardSummary> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(Summary);
    }
}

public sealed class FakeGrowthAnalyticsReader : IGrowthAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }

    public GrowthAnalyticsResult Result { get; set; } = new(
        DateTime.UtcNow, DateTime.UtcNow, GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0),
        GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0),
        GrowthMetric.Compute(0, 0), GrowthMetric.Compute(0, 0), null);

    public Task<GrowthAnalyticsResult> GetGrowthAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Result);
    }
}

public sealed class FakeRideAnalyticsReader : IRideAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public RideAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, 0m, 0m, 0m, 0m);

    public Task<RideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeFleetAnalyticsReader : IFleetAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public FleetAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, 0);

    public Task<FleetAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeSubscriptionAnalyticsReader : ISubscriptionAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public SubscriptionAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0);

    public Task<SubscriptionAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeAdvertisingAnalyticsReader : IAdvertisingAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public AdvertisingAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0);

    public Task<AdvertisingAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeMaintenanceAnalyticsReader : IMaintenanceAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public MaintenanceAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0m, 0m, 0m);

    public Task<MaintenanceAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeRoadsideAnalyticsReader : IRoadsideAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public RoadsideAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0m, 0m, 0m);

    public Task<RoadsideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeSupportAnalyticsReader : ISupportAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public SupportAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, 0, 0m);

    public Task<SupportAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}

public sealed class FakeFinancialAnalyticsReader : IFinancialAnalyticsReader
{
    public (DateTime FromUtc, DateTime ToUtc)? LastRequest { get; private set; }
    public FinancialAnalyticsSummary Summary { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow, 0m, 0m, GrowthMetric.Compute(0, 0));

    public Task<FinancialAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        LastRequest = (fromUtc, toUtc);
        return Task.FromResult(Summary);
    }
}
