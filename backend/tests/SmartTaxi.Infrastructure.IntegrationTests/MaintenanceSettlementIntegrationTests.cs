using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Maintenance;
using SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Maintenance.Repositories;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Mirrors SettleCampaignBudgetIntegrationTests exactly — the same hardened Module 8 settlement-recovery design proven against real Postgres, applied to Maintenance from day one.</summary>
[Collection("SharedPostgres")]
public class MaintenanceSettlementIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public MaintenanceSettlementIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<MaintenanceRequest> CreateCompletedRequestWithFinalCostAsync(decimal finalCost)
    {
        var ownerId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).AddAsync(vehicle, CancellationToken.None);
        }

        var request = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new MaintenanceRequestRepository(context).TryAddAsync(request, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new MaintenanceRequestRepository(context).TryTransitionAsync(
                request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.Completed, null, null, null, finalCost,
                null, null, DateTime.UtcNow, CancellationToken.None);
        }

        return request;
    }

    [Fact]
    public async Task Handle_NormalSettlement_PostsOneLedgerEntryAndMarksSettled()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(150m);

        await using var context = _fixture.CreateContext();
        var requestRepository = new MaintenanceRequestRepository(context);
        var billingService = new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
        var handler = new SettleMaintenanceRequestCommandHandler(requestRepository, billingService);

        var result = await handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);

        var entries = await readContext.FinancialLedgerEntries
            .Where(e => e.SourceType == "MaintenanceRequest" && e.SourceId == request.Id).ToListAsync();
        Assert.Single(entries);
        Assert.Equal(LedgerEntryType.MaintenanceRevenue, entries[0].EntryType);
    }

    [Fact]
    public async Task Handle_DuplicateSettlementAttempt_ReturnsConflictWithoutPostingAgain()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(150m);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleMaintenanceRequestCommandHandler(
                new MaintenanceRequestRepository(context), new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var first = await handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(first.IsSuccess);
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleMaintenanceRequestCommandHandler(
                new MaintenanceRequestRepository(context), new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var second = await handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.False(second.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "MaintenanceRequest" && e.SourceId == request.Id);
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task Handle_ConcurrentSettlementRequests_PostsExactlyOneLedgerEntry()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(150m);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var handler1 = new SettleMaintenanceRequestCommandHandler(
            new MaintenanceRequestRepository(context1), new MaintenanceBillingService(new FinancialAccountRepository(context1), new FinancialLedgerRepository(context1)));
        var handler2 = new SettleMaintenanceRequestCommandHandler(
            new MaintenanceRequestRepository(context2), new MaintenanceBillingService(new FinancialAccountRepository(context2), new FinancialLedgerRepository(context2)));

        var results = await Task.WhenAll(
            handler1.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None),
            handler2.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains(results, r => r.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "MaintenanceRequest" && e.SourceId == request.Id);
        Assert.Equal(1, entryCount);
    }

    /// <summary>The exact crash scenario: the ledger entry is posted directly through the real billing service (as PostBatchAsync would have already committed before a crash), but SettledAtUtc is deliberately never set. The handler's next invocation must converge without posting a second entry.</summary>
    [Fact]
    public async Task Handle_SimulatedCrashBetweenLedgerPostAndSettledFlag_ConvergesOnRetryWithoutDoublePosting()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(150m);

        await using (var context = _fixture.CreateContext())
        {
            var billingService = new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
            var posted = await billingService.TrySettleRequestAsync(
                request.Id, request.OwnerUserId, request.GarageUserId, 150m, "TND", DateTime.UtcNow, CancellationToken.None);
            Assert.True(posted);
        }

        await using (var preCheckContext = _fixture.CreateContext())
        {
            var reloaded = await new MaintenanceRequestRepository(preCheckContext).GetByIdAsync(request.Id, CancellationToken.None);
            Assert.Null(reloaded!.SettledAtUtc); // still desynced, as if the process crashed here
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleMaintenanceRequestCommandHandler(
                new MaintenanceRequestRepository(context), new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var final = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(final!.SettledAtUtc);

        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "MaintenanceRequest" && e.SourceId == request.Id);
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task Handle_FailureBeforeLedgerCommit_LeavesRequestUnsettledAndRetryable()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(150m);

        await using var readContext = _fixture.CreateContext();
        var beforeRetry = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Null(beforeRetry!.SettledAtUtc);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleMaintenanceRequestCommandHandler(
                new MaintenanceRequestRepository(context), new MaintenanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleMaintenanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using var finalReadContext = _fixture.CreateContext();
        var reloaded = await new MaintenanceRequestRepository(finalReadContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }
}
