using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

public interface IBusinessCustomerRepository
{
    Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken);

    Task<BusinessCustomer?> GetByIdAsync(Guid businessCustomerId, CancellationToken cancellationToken);

    Task<PagedResult<BusinessCustomer>> GetAllAsync(
        BusinessCustomerStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryReactivateAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCloseAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded by CurrentCreditUsage + amount &lt;= CreditLimit — the only place a deferred-billing amount can ever exceed the credit limit is here, and it never can.</summary>
    Task<bool> TryIncreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDecreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken);
}
