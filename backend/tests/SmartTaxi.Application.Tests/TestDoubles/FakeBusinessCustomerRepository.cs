using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeBusinessCustomerRepository : IBusinessCustomerRepository
{
    private readonly Dictionary<Guid, BusinessCustomer> _customersById = new();

    public Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken)
    {
        _customersById[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task<BusinessCustomer?> GetByIdAsync(Guid businessCustomerId, CancellationToken cancellationToken) =>
        Task.FromResult(_customersById.GetValueOrDefault(businessCustomerId));

    public Task<PagedResult<BusinessCustomer>> GetAllAsync(
        BusinessCustomerStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _customersById.Values.AsEnumerable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<BusinessCustomer>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TrySuspendAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(businessCustomerId, BusinessCustomerStatus.Active, BusinessCustomerStatus.Suspended, utcNow);

    public Task<bool> TryReactivateAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(businessCustomerId, BusinessCustomerStatus.Suspended, BusinessCustomerStatus.Active, utcNow);

    public Task<bool> TryCloseAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_customersById.TryGetValue(businessCustomerId, out var customer) || customer.Status == BusinessCustomerStatus.Closed)
        {
            return Task.FromResult(false);
        }

        SetProperty(customer, nameof(BusinessCustomer.Status), BusinessCustomerStatus.Closed);
        SetProperty(customer, nameof(BusinessCustomer.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryIncreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_customersById.TryGetValue(businessCustomerId, out var customer) || customer.CurrentCreditUsage + amount > customer.CreditLimit)
        {
            return Task.FromResult(false);
        }

        SetProperty(customer, nameof(BusinessCustomer.CurrentCreditUsage), customer.CurrentCreditUsage + amount);
        SetProperty(customer, nameof(BusinessCustomer.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryDecreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_customersById.TryGetValue(businessCustomerId, out var customer))
        {
            return Task.FromResult(false);
        }

        SetProperty(customer, nameof(BusinessCustomer.CurrentCreditUsage), Math.Max(0, customer.CurrentCreditUsage - amount));
        SetProperty(customer, nameof(BusinessCustomer.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransition(Guid businessCustomerId, BusinessCustomerStatus from, BusinessCustomerStatus to, DateTime utcNow)
    {
        if (!_customersById.TryGetValue(businessCustomerId, out var customer) || customer.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(customer, nameof(BusinessCustomer.Status), to);
        SetProperty(customer, nameof(BusinessCustomer.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(BusinessCustomer customer, string propertyName, object? value) =>
        typeof(BusinessCustomer).GetProperty(propertyName)!.SetValue(customer, value);
}
