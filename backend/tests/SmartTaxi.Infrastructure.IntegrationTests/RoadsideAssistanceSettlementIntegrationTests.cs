using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.RoadsideAssistance;
using SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Payments.Repositories;
using SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Mirrors MaintenanceSettlementIntegrationTests exactly — the same hardened Module 8/9 settlement-recovery design proven against real Postgres, applied to Roadside Assistance from day one.</summary>
[Collection("SharedPostgres")]
public class RoadsideAssistanceSettlementIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public RoadsideAssistanceSettlementIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<RoadsideAssistanceRequest> CreateCompletedRequestWithFinalCostAsync(decimal finalCost)
    {
        var ownerId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).AddAsync(vehicle, CancellationToken.None);
        }

        var request = RoadsideAssistanceRequest.Create(
            ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id, null, RoadsideServiceType.BatteryJumpStart, RoadsideUrgency.Low, "Batterie",
            36.8, 10.18, null, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoadsideAssistanceRequestRepository(context);
            await repository.TryAddAsync(request, CancellationToken.None);
            await repository.TrySelectPartnerAsync(request.Id, ownerId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
            await repository.TryRespondAsync(
                request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
            await repository.TryTransitionAsync(
                request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.Completed, null, partnerUserId, finalCost, null, null,
                false, DateTime.UtcNow, CancellationToken.None);
        }

        // Re-read: the atomic ExecuteUpdateAsync calls above never mutate this in-memory instance (e.g.
        // SelectedPartnerUserId), so the caller must see the DB's authoritative row.
        await using var readContext = _fixture.CreateContext();
        return (await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None))!;
    }

    [Fact]
    public async Task Handle_NormalSettlement_PostsOneLedgerEntryAndMarksSettled()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(60m);

        await using var context = _fixture.CreateContext();
        var requestRepository = new RoadsideAssistanceRequestRepository(context);
        var billingService = new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
        var handler = new SettleRoadsideAssistanceRequestCommandHandler(requestRepository, billingService);

        var result = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);

        var entries = await readContext.FinancialLedgerEntries
            .Where(e => e.SourceType == "RoadsideAssistanceRequest" && e.SourceId == request.Id).ToListAsync(CancellationToken.None);
        Assert.Single(entries);
        Assert.Equal(LedgerEntryType.RoadsideAssistanceRevenue, entries[0].EntryType);
    }

    [Fact]
    public async Task Handle_DuplicateSettlementAttempt_ReturnsConflictWithoutPostingAgain()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(60m);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleRoadsideAssistanceRequestCommandHandler(
                new RoadsideAssistanceRequestRepository(context),
                new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var first = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(first.IsSuccess);
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleRoadsideAssistanceRequestCommandHandler(
                new RoadsideAssistanceRequestRepository(context),
                new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var second = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.False(second.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "RoadsideAssistanceRequest" && e.SourceId == request.Id, CancellationToken.None);
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task Handle_ConcurrentSettlementRequests_PostsExactlyOneLedgerEntry()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(60m);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var handler1 = new SettleRoadsideAssistanceRequestCommandHandler(
            new RoadsideAssistanceRequestRepository(context1),
            new RoadsideAssistanceBillingService(new FinancialAccountRepository(context1), new FinancialLedgerRepository(context1)));
        var handler2 = new SettleRoadsideAssistanceRequestCommandHandler(
            new RoadsideAssistanceRequestRepository(context2),
            new RoadsideAssistanceBillingService(new FinancialAccountRepository(context2), new FinancialLedgerRepository(context2)));

        var results = await Task.WhenAll(
            handler1.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None),
            handler2.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains(results, r => r.IsSuccess);

        await using var readContext = _fixture.CreateContext();
        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "RoadsideAssistanceRequest" && e.SourceId == request.Id, CancellationToken.None);
        Assert.Equal(1, entryCount);
    }

    /// <summary>The exact crash scenario: the ledger entry is posted directly through the real billing service (as PostBatchAsync would have already committed before a crash), but SettledAtUtc is deliberately never set. The handler's next invocation must converge without posting a second entry.</summary>
    [Fact]
    public async Task Handle_SimulatedCrashBetweenLedgerPostAndSettledFlag_ConvergesOnRetryWithoutDoublePosting()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(60m);

        await using (var context = _fixture.CreateContext())
        {
            var billingService = new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context));
            var posted = await billingService.TrySettleRequestAsync(
                request.Id, request.RequesterUserId, request.RequesterRole, request.SelectedPartnerUserId!.Value, 60m, "TND", DateTime.UtcNow,
                CancellationToken.None);
            Assert.True(posted);
        }

        await using (var preCheckContext = _fixture.CreateContext())
        {
            var reloaded = await new RoadsideAssistanceRequestRepository(preCheckContext).GetByIdAsync(request.Id, CancellationToken.None);
            Assert.Null(reloaded!.SettledAtUtc); // still desynced, as if the process crashed here
        }

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleRoadsideAssistanceRequestCommandHandler(
                new RoadsideAssistanceRequestRepository(context),
                new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using var readContext = _fixture.CreateContext();
        var final = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(final!.SettledAtUtc);

        var entryCount = await readContext.FinancialLedgerEntries
            .CountAsync(e => e.SourceType == "RoadsideAssistanceRequest" && e.SourceId == request.Id, CancellationToken.None);
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task Handle_FailureBeforeLedgerCommit_LeavesRequestUnsettledAndRetryable()
    {
        var request = await CreateCompletedRequestWithFinalCostAsync(60m);

        await using var readContext = _fixture.CreateContext();
        var beforeRetry = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Null(beforeRetry!.SettledAtUtc);

        await using (var context = _fixture.CreateContext())
        {
            var handler = new SettleRoadsideAssistanceRequestCommandHandler(
                new RoadsideAssistanceRequestRepository(context),
                new RoadsideAssistanceBillingService(new FinancialAccountRepository(context), new FinancialLedgerRepository(context)));
            var result = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid()), CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        await using var finalReadContext = _fixture.CreateContext();
        var reloaded = await new RoadsideAssistanceRequestRepository(finalReadContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(reloaded!.SettledAtUtc);
    }
}
