using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves the Pending -> Confirmed / Pending -> Failed atomic guards against a real PostgreSQL database.</summary>
[Collection("SharedPostgres")]
public class SubscriptionChargeRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public SubscriptionChargeRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFields()
    {
        var subscriberId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var charge = SubscriptionCharge.Create(subscriberId, subscriptionId, 49.90m, "TND", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionChargeRepository(writeContext).AddAsync(charge, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionChargeRepository(readContext).GetByIdAsync(charge.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(subscriberId, reloaded!.SubscriberId);
        Assert.Equal(subscriptionId, reloaded.SubscriptionId);
        Assert.Equal(49.90m, reloaded.Amount);
        Assert.Equal("TND", reloaded.Currency);
        Assert.Equal(SubscriptionChargeStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task TryConfirmAsync_FromPending_Succeeds()
    {
        var charge = SubscriptionCharge.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "TND", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionChargeRepository(writeContext).AddAsync(charge, CancellationToken.None);
        }

        await using var confirmContext = _fixture.CreateContext();
        var confirmed = await new SubscriptionChargeRepository(confirmContext)
            .TryConfirmAsync(charge.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.True(confirmed);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionChargeRepository(readContext).GetByIdAsync(charge.Id, CancellationToken.None);
        Assert.Equal(SubscriptionChargeStatus.Confirmed, reloaded!.Status);
        Assert.NotNull(reloaded.ConfirmedAt);
    }

    [Fact]
    public async Task TryConfirmAsync_AlreadyConfirmed_FailsAndStaysConsistent()
    {
        var charge = SubscriptionCharge.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "TND", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionChargeRepository(writeContext).AddAsync(charge, CancellationToken.None);
        }

        await using (var firstConfirmContext = _fixture.CreateContext())
        {
            await new SubscriptionChargeRepository(firstConfirmContext)
                .TryConfirmAsync(charge.Id, DateTime.UtcNow, CancellationToken.None);
        }

        await using var secondConfirmContext = _fixture.CreateContext();
        var secondConfirm = await new SubscriptionChargeRepository(secondConfirmContext)
            .TryConfirmAsync(charge.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.False(secondConfirm);
    }

    [Fact]
    public async Task TryFailAsync_FromPending_RecordsReason()
    {
        var charge = SubscriptionCharge.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "TND", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SubscriptionChargeRepository(writeContext).AddAsync(charge, CancellationToken.None);
        }

        await using var failContext = _fixture.CreateContext();
        var failed = await new SubscriptionChargeRepository(failContext)
            .TryFailAsync(charge.Id, "Carte refusée.", DateTime.UtcNow, CancellationToken.None);

        Assert.True(failed);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new SubscriptionChargeRepository(readContext).GetByIdAsync(charge.Id, CancellationToken.None);
        Assert.Equal(SubscriptionChargeStatus.Failed, reloaded!.Status);
        Assert.Equal("Carte refusée.", reloaded.FailureReason);
    }

    [Fact]
    public async Task GetForSubscriptionAsync_ReturnsAllChargesNewestFirst()
    {
        var subscriptionId = Guid.NewGuid();
        var first = SubscriptionCharge.Create(Guid.NewGuid(), subscriptionId, 10m, "TND", DateTime.UtcNow.AddDays(-30));
        var second = SubscriptionCharge.Create(Guid.NewGuid(), subscriptionId, 10m, "TND", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new SubscriptionChargeRepository(writeContext);
            await repository.AddAsync(first, CancellationToken.None);
            await repository.AddAsync(second, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var charges = await new SubscriptionChargeRepository(readContext)
            .GetForSubscriptionAsync(subscriptionId, CancellationToken.None);

        Assert.Equal(2, charges.Count);
        Assert.Equal(second.Id, charges.First().Id);
    }
}
