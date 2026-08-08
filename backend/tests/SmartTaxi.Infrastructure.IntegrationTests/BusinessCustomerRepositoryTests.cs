using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that BusinessCustomerRepository's
/// credit-limit guard — "a BusinessCustomer cannot exceed its credit limit" —
/// actually holds atomically under concurrency, which FakeBusinessCustomerRepository's
/// in-memory single-threaded check cannot exercise.
/// </summary>
[Collection("SharedPostgres")]
public class BusinessCustomerRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public BusinessCustomerRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static BusinessCustomer NewCustomer(decimal creditLimit) => BusinessCustomer.Register(
        $"Société {Guid.NewGuid():N}", $"TAX-{Guid.NewGuid():N}"[..20], "1 Rue de la Paix, Tunis", "Amira Ben Salah",
        "amira@example.com", null, DeferredPaymentTerm.Days15, creditLimit, DateTime.UtcNow);

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFields()
    {
        var customer = NewCustomer(5000m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerRepository(writeContext).AddAsync(customer, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new BusinessCustomerRepository(readContext).GetByIdAsync(customer.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(BusinessCustomerStatus.Active, reloaded!.Status);
        Assert.Equal(5000m, reloaded.RemainingCredit);
    }

    [Fact]
    public async Task TryIncreaseCreditUsageAsync_UpToTheLimit_Succeeds_BeyondItFails()
    {
        var customer = NewCustomer(1000m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerRepository(writeContext).AddAsync(customer, CancellationToken.None);
        }

        await using (var contextOne = _fixture.CreateContext())
        {
            var firstIncrease = await new BusinessCustomerRepository(contextOne)
                .TryIncreaseCreditUsageAsync(customer.Id, 700m, DateTime.UtcNow, CancellationToken.None);
            Assert.True(firstIncrease);
        }

        await using (var contextTwo = _fixture.CreateContext())
        {
            // 700 already used; 400 more would push it to 1100, exceeding the 1000 limit.
            var secondIncrease = await new BusinessCustomerRepository(contextTwo)
                .TryIncreaseCreditUsageAsync(customer.Id, 400m, DateTime.UtcNow, CancellationToken.None);
            Assert.False(secondIncrease);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.BusinessCustomers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(700m, reloaded.CurrentCreditUsage);
    }

    [Fact]
    public async Task ConcurrentTryIncreaseCreditUsageAsync_TogetherExceedingLimit_OnlyOneSucceeds()
    {
        var customer = NewCustomer(1000m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerRepository(writeContext).AddAsync(customer, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new BusinessCustomerRepository(contextA).TryIncreaseCreditUsageAsync(customer.Id, 700m, DateTime.UtcNow, CancellationToken.None),
            new BusinessCustomerRepository(contextB).TryIncreaseCreditUsageAsync(customer.Id, 700m, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.BusinessCustomers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(700m, reloaded.CurrentCreditUsage);
        Assert.True(reloaded.CurrentCreditUsage <= reloaded.CreditLimit);
    }

    [Fact]
    public async Task TryDecreaseCreditUsageAsync_ClampsAtZero_NeverGoesNegative()
    {
        var customer = NewCustomer(1000m);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new BusinessCustomerRepository(writeContext);
            await repository.AddAsync(customer, CancellationToken.None);
            await repository.TryIncreaseCreditUsageAsync(customer.Id, 200m, DateTime.UtcNow, CancellationToken.None);
        }

        await using (var decreaseContext = _fixture.CreateContext())
        {
            var decreased = await new BusinessCustomerRepository(decreaseContext)
                .TryDecreaseCreditUsageAsync(customer.Id, 500m, DateTime.UtcNow, CancellationToken.None);
            Assert.True(decreased);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.BusinessCustomers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(0m, reloaded.CurrentCreditUsage);
    }

    [Fact]
    public async Task DuplicateTaxIdentifier_IsRejectedByTheUniqueIndex()
    {
        var taxId = $"TAX-{Guid.NewGuid():N}"[..20];
        var first = BusinessCustomer.Register("Société A", taxId, "Adresse A", "Contact A", "a@example.com", null, DeferredPaymentTerm.Immediate, 100m, DateTime.UtcNow);
        var second = BusinessCustomer.Register("Société B", taxId, "Adresse B", "Contact B", "b@example.com", null, DeferredPaymentTerm.Immediate, 100m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerRepository(writeContext).AddAsync(first, CancellationToken.None);
        }

        await using var duplicateContext = _fixture.CreateContext();
        await Assert.ThrowsAsync<DbUpdateException>(() => new BusinessCustomerRepository(duplicateContext).AddAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task SuspendThenReactivate_FullLifecycle_TransitionsCorrectly()
    {
        var customer = NewCustomer(500m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerRepository(writeContext).AddAsync(customer, CancellationToken.None);
        }

        await using (var suspendContext = _fixture.CreateContext())
        {
            Assert.True(await new BusinessCustomerRepository(suspendContext).TrySuspendAsync(customer.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using (var reactivateContext = _fixture.CreateContext())
        {
            Assert.True(await new BusinessCustomerRepository(reactivateContext).TryReactivateAsync(customer.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.BusinessCustomers.SingleAsync(c => c.Id == customer.Id);
        Assert.Equal(BusinessCustomerStatus.Active, reloaded.Status);
    }
}
