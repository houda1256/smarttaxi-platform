using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that CashRegisterSessionRepository's
/// "only one Open session per register" guarantee and cash-movement summation
/// actually hold — the Fake repository used by Application-layer tests has no
/// real unique-index or SQL-aggregation semantics to exercise.
/// </summary>
[Collection("SharedPostgres")]
public class CashRegisterRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public CashRegisterRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> RegisterCashBoxAsync(Guid ownerId)
    {
        await using var context = _fixture.CreateContext();
        var cashRegister = CashRegisterBox.Register(ownerId, "Agence Centre-Ville", DateTime.UtcNow);
        await new CashRegisterRepository(context).AddAsync(cashRegister, CancellationToken.None);
        return cashRegister.Id;
    }

    [Fact]
    public async Task TryAddAsync_RoundTripsSession()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);

        var session = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var added = await new CashRegisterSessionRepository(writeContext).TryAddAsync(session, CancellationToken.None);
            Assert.True(added);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new CashRegisterSessionRepository(readContext).GetByIdAsync(session.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(CashRegisterSessionStatus.Open, reloaded!.Status);
        Assert.Equal(100m, reloaded.OpeningBalance);
    }

    [Fact]
    public async Task ConcurrentTryAddAsync_ForSameRegister_OnlyOneOpenSessionSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);

        var sessionA = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);
        var sessionB = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new CashRegisterSessionRepository(contextA).TryAddAsync(sessionA, CancellationToken.None),
            new CashRegisterSessionRepository(contextB).TryAddAsync(sessionB, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var openCount = await readContext.CashRegisterSessions
            .CountAsync(s => s.CashRegisterId == cashRegisterId && s.Status == CashRegisterSessionStatus.Open);
        Assert.Equal(1, openCount);
    }

    [Fact]
    public async Task AfterClosingASession_ANewSessionCanBeOpenedForTheSameRegister()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);
        var firstSession = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new CashRegisterSessionRepository(writeContext);
            await repository.TryAddAsync(firstSession, CancellationToken.None);
            await repository.TryCloseAsync(firstSession.Id, 100m, 100m, 0m, null, DateTime.UtcNow, CancellationToken.None);
        }

        var secondSession = CashRegisterSession.Open(cashRegisterId, ownerId, 0m, DateTime.UtcNow);

        await using (var secondContext = _fixture.CreateContext())
        {
            var added = await new CashRegisterSessionRepository(secondContext).TryAddAsync(secondSession, CancellationToken.None);
            Assert.True(added);
        }
    }

    [Fact]
    public async Task CloseCashRegisterSession_WithoutJustification_KeepsSessionOpen()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);
        var session = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new CashRegisterSessionRepository(writeContext).TryAddAsync(session, CancellationToken.None);
        }

        // The DB layer itself allows any TryCloseAsync call — "reason required for a difference" is
        // enforced by CloseCashRegisterSessionCommandHandler before it ever calls the repository. This
        // test only proves TryCloseAsync writes exactly the values it's given, atomically.
        await using (var closeContext = _fixture.CreateContext())
        {
            var closed = await new CashRegisterSessionRepository(closeContext)
                .TryCloseAsync(session.Id, closingExpectedBalance: 150m, closingActualBalance: 140m, difference: -10m, "Erreur de caisse", DateTime.UtcNow, CancellationToken.None);
            Assert.True(closed);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.CashRegisterSessions.SingleAsync(s => s.Id == session.Id);
        Assert.Equal(CashRegisterSessionStatus.Closed, reloaded.Status);
        Assert.Equal(-10m, reloaded.Difference);
        Assert.Equal("Erreur de caisse", reloaded.DifferenceReason);
    }

    [Fact]
    public async Task ReconcileThenDispute_OnlyFirstTransitionSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);
        var session = CashRegisterSession.Open(cashRegisterId, ownerId, 100m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new CashRegisterSessionRepository(writeContext);
            await repository.TryAddAsync(session, CancellationToken.None);
            await repository.TryCloseAsync(session.Id, 100m, 100m, 0m, null, DateTime.UtcNow, CancellationToken.None);
        }

        await using (var reconcileContext = _fixture.CreateContext())
        {
            var reconciled = await new CashRegisterSessionRepository(reconcileContext).TryReconcileAsync(session.Id, DateTime.UtcNow, CancellationToken.None);
            Assert.True(reconciled);
        }

        // Now Reconciled — disputing must fail, since TryDisputeAsync is guarded on Status == Closed.
        await using (var disputeContext = _fixture.CreateContext())
        {
            var disputed = await new CashRegisterSessionRepository(disputeContext)
                .TryDisputeAsync(session.Id, "Litige tardif", DateTime.UtcNow, CancellationToken.None);
            Assert.False(disputed);
        }
    }

    [Fact]
    public async Task CashMovements_NetTotal_SumsIncomeAndExpenseCorrectly()
    {
        var ownerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);
        var session = CashRegisterSession.Open(cashRegisterId, ownerId, 0m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new CashRegisterSessionRepository(writeContext).TryAddAsync(session, CancellationToken.None);
        }

        await using (var movementContext = _fixture.CreateContext())
        {
            var repository = new CashMovementRepository(movementContext);
            await repository.AddAsync(new CashMovement(session.Id, CashMovementType.RideIncome, 80m, "Course 1", ownerId, DateTime.UtcNow), CancellationToken.None);
            await repository.AddAsync(new CashMovement(session.Id, CashMovementType.RideIncome, 60m, "Course 2", ownerId, DateTime.UtcNow), CancellationToken.None);
            await repository.AddAsync(new CashMovement(session.Id, CashMovementType.DriverPayment, -40m, "Paiement chauffeur", ownerId, DateTime.UtcNow), CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var netTotal = await new CashMovementRepository(readContext).GetNetTotalForSessionAsync(session.Id, CancellationToken.None);
        Assert.Equal(100m, netTotal);

        var movements = await new CashMovementRepository(readContext).GetForSessionAsync(session.Id, CancellationToken.None);
        Assert.Equal(3, movements.Count);
    }

    [Fact]
    public async Task GetForOwnerAsync_JoinsThroughCashRegisterBox_ReturnsOnlyThatOwnersSessions()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var cashRegisterId = await RegisterCashBoxAsync(ownerId);
        var otherCashRegisterId = await RegisterCashBoxAsync(otherOwnerId);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new CashRegisterSessionRepository(writeContext);
            await repository.TryAddAsync(CashRegisterSession.Open(cashRegisterId, ownerId, 0m, DateTime.UtcNow), CancellationToken.None);
            await repository.TryAddAsync(CashRegisterSession.Open(otherCashRegisterId, otherOwnerId, 0m, DateTime.UtcNow), CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var result = await new CashRegisterSessionRepository(readContext).GetForOwnerAsync(ownerId, null, 1, 10, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(cashRegisterId, result.Items.Single().CashRegisterId);
    }
}
