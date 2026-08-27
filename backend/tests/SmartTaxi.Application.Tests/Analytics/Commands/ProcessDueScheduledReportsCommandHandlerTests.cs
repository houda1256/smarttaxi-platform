using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Commands.ProcessDueScheduledReports;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Tests.Analytics.Commands;

public class ProcessDueScheduledReportsCommandHandlerTests
{
    private static ProcessDueScheduledReportsCommandHandler BuildHandler(
        FakeScheduledReportRepository repository, FakeNotificationDispatcher notificationDispatcher,
        IRideAnalyticsReader? rideReader = null) =>
        new(
            repository, new FakeAdminDashboardReader(), new FakeGrowthAnalyticsReader(), rideReader ?? new FakeRideAnalyticsReader(),
            new FakeFleetAnalyticsReader(), new FakeAdvertisingAnalyticsReader(), new FakeSupportAnalyticsReader(),
            new FakeFinancialAnalyticsReader(), notificationDispatcher);

    private async Task<Guid> CreateDueDefinitionAsync(FakeScheduledReportRepository repository, ScheduledReportCategory category)
    {
        var definition = ScheduledReportDefinition.Create(
            category, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddDays(-1));
        await repository.TryAddAsync(definition, CancellationToken.None);
        return definition.Id;
    }

    [Fact]
    public async Task Handle_DueActiveDefinition_ProcessesAndAdvancesSchedule()
    {
        var repository = new FakeScheduledReportRepository();
        var notificationDispatcher = new FakeNotificationDispatcher();
        var id = await CreateDueDefinitionAsync(repository, ScheduledReportCategory.Ride);
        var originalNextRun = (await repository.GetByIdAsync(id, CancellationToken.None))!.NextRunAtUtc;

        var handler = BuildHandler(repository, notificationDispatcher);
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), CancellationToken.None);

        Assert.Equal(1, processedCount);
        var reloaded = await repository.GetByIdAsync(id, CancellationToken.None);
        Assert.Null(reloaded!.ProcessingClaimedAtUtc);
        Assert.NotNull(reloaded.LastProcessedAtUtc);
        Assert.Equal(originalNextRun.AddDays(1), reloaded.NextRunAtUtc);
        Assert.Single(notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_NotYetDueDefinition_IsSkipped()
    {
        var repository = new FakeScheduledReportRepository();
        var definition = ScheduledReportDefinition.Create(
            ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow);
        await repository.TryAddAsync(definition, CancellationToken.None);

        var handler = BuildHandler(repository, new FakeNotificationDispatcher());
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), CancellationToken.None);

        Assert.Equal(0, processedCount);
    }

    [Fact]
    public async Task Handle_InactiveDueDefinition_IsNeverProcessed()
    {
        var repository = new FakeScheduledReportRepository();
        var id = await CreateDueDefinitionAsync(repository, ScheduledReportCategory.Ride);
        await repository.TryDeactivateAsync(id, DateTime.UtcNow, CancellationToken.None);

        var handler = BuildHandler(repository, new FakeNotificationDispatcher());
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), CancellationToken.None);

        Assert.Equal(0, processedCount);
    }

    [Fact]
    public async Task Handle_TwoConcurrentClaimsOnSameDueDefinition_OnlyOneWins()
    {
        var repository = new FakeScheduledReportRepository();
        var id = await CreateDueDefinitionAsync(repository, ScheduledReportCategory.Ride);
        var utcNow = DateTime.UtcNow;

        var firstClaim = await repository.TryClaimAsync(id, utcNow, CancellationToken.None);
        var secondClaim = await repository.TryClaimAsync(id, utcNow.AddSeconds(1), CancellationToken.None);

        Assert.True(firstClaim);
        Assert.False(secondClaim);
    }

    [Fact]
    public async Task Handle_ExceptionDuringGeneration_LeavesClaimInPlaceForRetry()
    {
        var repository = new FakeScheduledReportRepository();
        var id = await CreateDueDefinitionAsync(repository, ScheduledReportCategory.Ride);
        var notificationDispatcher = new FakeNotificationDispatcher();
        var throwingRideReader = new ThrowingRideAnalyticsReader();

        var handler = BuildHandler(repository, notificationDispatcher, throwingRideReader);
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), CancellationToken.None);

        Assert.Equal(0, processedCount);
        var reloaded = await repository.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(reloaded!.ProcessingClaimedAtUtc);
        Assert.Null(reloaded.LastProcessedAtUtc);
        Assert.Empty(notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_StaleClaimFromCrashedProcessor_BecomesReclaimable()
    {
        var repository = new FakeScheduledReportRepository();
        var id = await CreateDueDefinitionAsync(repository, ScheduledReportCategory.Ride);
        var staleClaimTime = DateTime.UtcNow - IScheduledReportRepository.StaleClaimThreshold - TimeSpan.FromMinutes(1);
        await repository.TryClaimAsync(id, staleClaimTime, CancellationToken.None);

        var handler = BuildHandler(repository, new FakeNotificationDispatcher());
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), CancellationToken.None);

        Assert.Equal(1, processedCount);
    }

    private sealed class ThrowingRideAnalyticsReader : IRideAnalyticsReader
    {
        public Task<RideAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");
    }
}
