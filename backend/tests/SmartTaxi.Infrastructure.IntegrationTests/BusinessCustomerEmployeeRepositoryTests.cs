using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class BusinessCustomerEmployeeRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public BusinessCustomerEmployeeRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> RegisterBusinessCustomerAsync()
    {
        await using var context = _fixture.CreateContext();
        var customer = BusinessCustomer.Register(
            $"Société {Guid.NewGuid():N}", $"TAX-{Guid.NewGuid():N}"[..20], "Adresse", "Contact", "contact@example.com",
            null, DeferredPaymentTerm.Days7, 1000m, DateTime.UtcNow);
        await new BusinessCustomerRepository(context).AddAsync(customer, CancellationToken.None);
        return customer.Id;
    }

    [Fact]
    public async Task AddAndGetActiveByUserId_RoundTrips()
    {
        var businessCustomerId = await RegisterBusinessCustomerAsync();
        var userId = Guid.NewGuid();
        var employee = new BusinessCustomerEmployee(
            businessCustomerId, userId, BusinessEmployeeRole.BusinessAdmin, 500m, "Sedan,SUV", null, null, "Tunis-Centre", 50m, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerEmployeeRepository(writeContext).AddAsync(employee, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new BusinessCustomerEmployeeRepository(readContext).GetActiveByUserIdAsync(userId, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(BusinessEmployeeRole.BusinessAdmin, reloaded!.Role);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task TryDeactivateAsync_HidesEmployeeFromGetActiveByUserId()
    {
        var businessCustomerId = await RegisterBusinessCustomerAsync();
        var userId = Guid.NewGuid();
        var employee = new BusinessCustomerEmployee(
            businessCustomerId, userId, BusinessEmployeeRole.BusinessEmployee, null, null, null, null, null, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new BusinessCustomerEmployeeRepository(writeContext).AddAsync(employee, CancellationToken.None);
        }

        await using (var deactivateContext = _fixture.CreateContext())
        {
            var deactivated = await new BusinessCustomerEmployeeRepository(deactivateContext)
                .TryDeactivateAsync(employee.Id, DateTime.UtcNow, CancellationToken.None);
            Assert.True(deactivated);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new BusinessCustomerEmployeeRepository(readContext).GetActiveByUserIdAsync(userId, CancellationToken.None);
        Assert.Null(reloaded);
    }

    [Fact]
    public async Task GetForBusinessCustomerAsync_ReturnsOnlyThatCompanysEmployees()
    {
        var businessCustomerIdOne = await RegisterBusinessCustomerAsync();
        var businessCustomerIdTwo = await RegisterBusinessCustomerAsync();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new BusinessCustomerEmployeeRepository(writeContext);
            await repository.AddAsync(
                new BusinessCustomerEmployee(businessCustomerIdOne, Guid.NewGuid(), BusinessEmployeeRole.BusinessManager, null, null, null, null, null, null, DateTime.UtcNow),
                CancellationToken.None);
            await repository.AddAsync(
                new BusinessCustomerEmployee(businessCustomerIdOne, Guid.NewGuid(), BusinessEmployeeRole.BusinessAccountant, null, null, null, null, null, null, DateTime.UtcNow),
                CancellationToken.None);
            await repository.AddAsync(
                new BusinessCustomerEmployee(businessCustomerIdTwo, Guid.NewGuid(), BusinessEmployeeRole.BusinessEmployee, null, null, null, null, null, null, DateTime.UtcNow),
                CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var employees = await new BusinessCustomerEmployeeRepository(readContext).GetForBusinessCustomerAsync(businessCustomerIdOne, CancellationToken.None);

        Assert.Equal(2, employees.Count);
        Assert.All(employees, e => Assert.Equal(businessCustomerIdOne, e.BusinessCustomerId));
    }
}
