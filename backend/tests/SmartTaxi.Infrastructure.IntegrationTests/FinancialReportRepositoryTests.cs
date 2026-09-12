using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Reports;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that FinancialReportRepository's
/// aggregations are real SQL SUMs over the actual tables, correctly scoped by
/// date range and actor — not fabricated numbers. Uses a unique ActorId per
/// test to stay isolated from every other test's data in the shared-Postgres
/// suite, since Payments/ledger rows accumulate globally across the whole run.
/// </summary>
[Collection("SharedPostgres")]
public class FinancialReportRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public FinancialReportRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetReportAsync_AggregatesLedgerEntries_ScopedByDateRangeAndActor()
    {
        var driverUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var inRangeTime = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var outOfRangeTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        await using (var writeContext = _fixture.CreateContext())
        {
            var ledgerRepository = new FinancialLedgerRepository(writeContext);
            var accountRepository = new FinancialAccountRepository(writeContext);

            var platform = await accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
            var driver = await accountRepository.GetOrCreateAsync(FinancialAccountType.Driver, driverUserId, "TND", CancellationToken.None);
            var owner = await accountRepository.GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerUserId, "TND", CancellationToken.None);

            var inRangeLines = new List<Application.Payments.Ledger.Abstractions.LedgerPostingLine>
            {
                new(platform.Id, platform.Id, 20m, LedgerEntryType.PlatformCommission, "Commission",
                    DebitEffects: [], CreditEffects: []),
                new(platform.Id, driver.Id, 60m, LedgerEntryType.DriverEarning, "Gain chauffeur",
                    DebitEffects: [], CreditEffects: []),
                new(platform.Id, owner.Id, 20m, LedgerEntryType.OwnerEarning, "Gain propriétaire",
                    DebitEffects: [], CreditEffects: [])
            };
            await ledgerRepository.PostBatchAsync("Payment", paymentId, "TND", null, inRangeTime, inRangeLines, CancellationToken.None);

            // A second, out-of-range posting for the same actor-adjacent data — proves the date filter
            // actually excludes it rather than summing everything ever posted.
            var outOfRangePaymentId = Guid.NewGuid();
            var outOfRangeLines = new List<Application.Payments.Ledger.Abstractions.LedgerPostingLine>
            {
                new(platform.Id, driver.Id, 999m, LedgerEntryType.DriverEarning, "Gain hors période",
                    DebitEffects: [], CreditEffects: [])
            };
            await ledgerRepository.PostBatchAsync("Payment", outOfRangePaymentId, "TND", null, outOfRangeTime, outOfRangeLines, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var repository = new FinancialReportRepository(readContext);

        var filter = new FinancialReportFilter(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc), driverUserId);

        var report = await repository.GetReportAsync(filter, CancellationToken.None);

        Assert.Equal(60m, report.DriverEarnings);
    }

    [Fact]
    public async Task GetReportAsync_AggregatesExpenses_ScopedByOwnerAndDateRange()
    {
        var ownerId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var inRangeExpense = Domain.Fleet.Expenses.Entities.FleetExpense.Create(
                ownerId, null, null, null, Domain.Fleet.Expenses.Enums.ExpenseCategory.Fuel,
                Domain.Fleet.Expenses.ValueObjects.Money.Create(75m, "TND"), new DateOnly(2026, 6, 10), "Carburant", null, ownerId, DateTime.UtcNow);

            await writeContext.FleetExpenses.AddAsync(inRangeExpense, CancellationToken.None);
            await writeContext.SaveChangesAsync(CancellationToken.None);

            await writeContext.FleetExpenses
                .Where(e => e.Id == inRangeExpense.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Status, Domain.Fleet.Expenses.Enums.ExpenseStatus.Approved));
        }

        await using var readContext = _fixture.CreateContext();
        var repository = new FinancialReportRepository(readContext);

        var filter = new FinancialReportFilter(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc), ownerId);

        var report = await repository.GetReportAsync(filter, CancellationToken.None);

        Assert.Equal(75m, report.Expenses);
    }

    [Fact]
    public async Task GetReportAsync_OutsideAnyDateRange_ReturnsAllZeros()
    {
        await using var readContext = _fixture.CreateContext();
        var repository = new FinancialReportRepository(readContext);

        // Impossibly narrow future window with a never-used ActorId — guarantees zero matches regardless
        // of what other tests have posted elsewhere in the shared database.
        var filter = new FinancialReportFilter(
            new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2099, 1, 2, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

        var report = await repository.GetReportAsync(filter, CancellationToken.None);

        Assert.Equal(0m, report.GrossRevenue);
        Assert.Equal(0m, report.DriverEarnings);
        Assert.Equal(0m, report.Expenses);
        Assert.Equal(0m, report.RefundAmount);
    }
}
