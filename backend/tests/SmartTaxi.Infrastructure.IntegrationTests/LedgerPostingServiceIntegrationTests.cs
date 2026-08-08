using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves — through real dependency injection, not direct `new`, and against a
/// real PostgreSQL database — that <see cref="ILedgerPostingService"/> and its
/// repository dependencies resolve and function correctly. This registers the
/// exact three service lines added to
/// <see cref="DependencyInjection.AddInfrastructure"/> for this module
/// (IFinancialAccountRepository, IFinancialLedgerRepository,
/// ILedgerPostingService); it stops short of invoking AddInfrastructure itself
/// only to avoid pulling in the full Identity/Fleet/Rides configuration
/// surface unrelated to this module. Before this module was implemented,
/// resolving ILedgerPostingService in production had no registration at all
/// for its two dependencies — this test is the regression guard for that:
/// ConfirmPaymentCommandHandler/RefundPaymentCommandHandler depend on exactly
/// this constructor-injection graph.
/// </summary>
[Collection("SharedPostgres")]
public class LedgerPostingServiceIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public LedgerPostingServiceIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private ServiceProvider BuildRealInfrastructureProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IFinancialLedgerRepository, FinancialLedgerRepository>();
        services.AddScoped<ILedgerPostingService, Application.Payments.Ledger.LedgerPostingService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ILedgerPostingService_ResolvesFromTheRealProductionContainer()
    {
        await using var provider = BuildRealInfrastructureProvider();
        using var scope = provider.CreateScope();

        var ledgerPostingService = scope.ServiceProvider.GetRequiredService<ILedgerPostingService>();
        var accountRepository = scope.ServiceProvider.GetRequiredService<IFinancialAccountRepository>();
        var ledgerRepository = scope.ServiceProvider.GetRequiredService<IFinancialLedgerRepository>();

        Assert.NotNull(ledgerPostingService);
        Assert.NotNull(accountRepository);
        Assert.NotNull(ledgerRepository);
    }

    [Fact]
    public async Task PostPaymentConfirmedAsync_ThroughTheRealContainer_DistributesTheFullFareAndZeroesPlatformPending()
    {
        // Platform is a database-wide singleton account shared with every other test in this
        // assembly, so its balance is asserted as a before/after delta, never an absolute value.
        Domain.Payments.Accounts.Entities.FinancialAccount platformBefore;
        await using (var beforeContext = _fixture.CreateContext())
        {
            platformBefore = await new FinancialAccountRepository(beforeContext)
                .GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
        }

        await using var provider = BuildRealInfrastructureProvider();
        using var scope = provider.CreateScope();

        var ledgerPostingService = scope.ServiceProvider.GetRequiredService<ILedgerPostingService>();
        var paymentId = Guid.NewGuid();
        var driverUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        await ledgerPostingService.PostPaymentConfirmedAsync(
            paymentId, driverUserId, ownerUserId, finalFareAmount: 100m, platformCommissionAmount: 20m,
            driverAmount: 60m, ownerAmount: 20m, currency: "TND", createdBy: null, utcNow, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var accountRepository = new FinancialAccountRepository(readContext);

        var platform = await accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        var driver = await accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);
        var owner = await accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.TaxiOwner, ownerUserId, CancellationToken.None);

        Assert.NotNull(platform);
        Assert.NotNull(driver);
        Assert.NotNull(owner);

        // The whole fare is fully distributed by construction: Platform.Pending nets to 0 after
        // PaymentCollected (+100) and PlatformCommission/DriverEarning/OwnerEarning (-20-60-20).
        Assert.Equal(0m, platform!.PendingBalance - platformBefore.PendingBalance);
        Assert.Equal(20m, platform.AvailableBalance - platformBefore.AvailableBalance);
        Assert.Equal(60m, driver!.AvailableBalance);
        Assert.Equal(20m, owner!.AvailableBalance);

        var entries = await readContext.FinancialLedgerEntries.Where(e => e.SourceType == "Payment" && e.SourceId == paymentId).ToListAsync();
        Assert.Equal(4, entries.Count);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.PaymentCollected);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.PlatformCommission);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.DriverEarning);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.OwnerEarning);
    }

    [Fact]
    public async Task PostPaymentConfirmedAsync_CalledTwiceForTheSamePayment_IsIdempotent()
    {
        await using var provider = BuildRealInfrastructureProvider();
        using var scope = provider.CreateScope();

        var ledgerPostingService = scope.ServiceProvider.GetRequiredService<ILedgerPostingService>();
        var paymentId = Guid.NewGuid();
        var driverUserId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        for (var i = 0; i < 2; i++)
        {
            await ledgerPostingService.PostPaymentConfirmedAsync(
                paymentId, driverUserId, Guid.Empty, finalFareAmount: 50m, platformCommissionAmount: 10m,
                driverAmount: 40m, ownerAmount: 0m, currency: "TND", createdBy: null, utcNow, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var driver = await new FinancialAccountRepository(readContext)
            .GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);

        Assert.Equal(40m, driver!.AvailableBalance);

        var entryCount = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Payment" && e.SourceId == paymentId);
        Assert.Equal(3, entryCount);
    }

    [Fact]
    public async Task PostRefundAsync_ThroughTheRealContainer_DebitsPlatformAvailableBalance()
    {
        Domain.Payments.Accounts.Entities.FinancialAccount platformBefore;
        await using (var beforeContext = _fixture.CreateContext())
        {
            platformBefore = await new FinancialAccountRepository(beforeContext)
                .GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
        }

        await using var provider = BuildRealInfrastructureProvider();
        using var scope = provider.CreateScope();

        var ledgerPostingService = scope.ServiceProvider.GetRequiredService<ILedgerPostingService>();
        var paymentId = Guid.NewGuid();
        var refundRecordId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        await ledgerPostingService.PostPaymentConfirmedAsync(
            paymentId, Guid.NewGuid(), Guid.Empty, finalFareAmount: 100m, platformCommissionAmount: 100m,
            driverAmount: 0m, ownerAmount: 0m, currency: "TND", createdBy: null, utcNow, CancellationToken.None);

        await ledgerPostingService.PostRefundAsync(refundRecordId, 30m, "TND", createdBy: null, utcNow, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var platform = await new FinancialAccountRepository(readContext)
            .GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);

        // +100 commission, then -30 refund => net +70 on Platform's Available bucket.
        Assert.Equal(70m, platform!.AvailableBalance - platformBefore.AvailableBalance);

        var refundEntries = await readContext.FinancialLedgerEntries.CountAsync(e => e.SourceType == "Refund" && e.SourceId == refundRecordId);
        Assert.Equal(1, refundEntries);
    }
}
