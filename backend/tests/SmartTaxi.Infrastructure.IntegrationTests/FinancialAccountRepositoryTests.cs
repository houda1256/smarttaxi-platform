using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that FinancialAccount's lazy
/// provisioning is safe under concurrency: the unique (AccountType,
/// OwnerReferenceId) index — and the extra filtered unique index for the
/// Platform singleton — must actually stop a duplicate row from ever
/// existing, not just in the Fake used by Application-layer unit tests.
/// </summary>
[Collection("SharedPostgres")]
public class FinancialAccountRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public FinancialAccountRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrCreateAsync_RoundTripsAndIsIdempotentOnSubsequentCalls()
    {
        var driverId = Guid.NewGuid();

        Guid firstAccountId;
        await using (var context = _fixture.CreateContext())
        {
            var account = await new FinancialAccountRepository(context)
                .GetOrCreateAsync(FinancialAccountType.Driver, driverId, "TND", CancellationToken.None);
            firstAccountId = account.Id;
        }

        await using var secondContext = _fixture.CreateContext();
        var second = await new FinancialAccountRepository(secondContext)
            .GetOrCreateAsync(FinancialAccountType.Driver, driverId, "TND", CancellationToken.None);

        Assert.Equal(firstAccountId, second.Id);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.FinancialAccounts.CountAsync(a => a.AccountType == FinancialAccountType.Driver && a.OwnerReferenceId == driverId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConcurrentGetOrCreateAsync_ForSameOwner_OnlyOneRowIsPersisted()
    {
        var ownerId = Guid.NewGuid();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new FinancialAccountRepository(contextA).GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerId, "TND", CancellationToken.None),
            new FinancialAccountRepository(contextB).GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerId, "TND", CancellationToken.None));

        Assert.Equal(results[0].Id, results[1].Id);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.FinancialAccounts.CountAsync(a => a.AccountType == FinancialAccountType.TaxiOwner && a.OwnerReferenceId == ownerId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConcurrentGetOrCreateAsync_ForPlatform_NeverCreatesTwoSingletonRows()
    {
        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new FinancialAccountRepository(contextA).GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None),
            new FinancialAccountRepository(contextB).GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None));

        Assert.Equal(results[0].Id, results[1].Id);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.FinancialAccounts.CountAsync(a => a.AccountType == FinancialAccountType.Platform);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetForOwnerReferenceAsync_ReturnsOnlyAccountsForThatOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new FinancialAccountRepository(writeContext);
            await repository.GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerId, "TND", CancellationToken.None);
            await repository.GetOrCreateAsync(FinancialAccountType.Driver, ownerId, "TND", CancellationToken.None);
            await repository.GetOrCreateAsync(FinancialAccountType.Driver, otherOwnerId, "TND", CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var accounts = await new FinancialAccountRepository(readContext).GetForOwnerReferenceAsync(ownerId, CancellationToken.None);

        Assert.Equal(2, accounts.Count);
        Assert.All(accounts, a => Assert.Equal(ownerId, a.OwnerReferenceId));
    }
}
