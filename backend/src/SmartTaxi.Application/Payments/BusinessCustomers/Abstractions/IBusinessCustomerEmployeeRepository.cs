using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;

public interface IBusinessCustomerEmployeeRepository
{
    Task AddAsync(BusinessCustomerEmployee employee, CancellationToken cancellationToken);

    Task<BusinessCustomerEmployee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken);

    /// <summary>A User is assumed to have at most one active BusinessCustomer membership at a time — the simplification this phase's scope allows.</summary>
    Task<BusinessCustomerEmployee?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BusinessCustomerEmployee>> GetForBusinessCustomerAsync(Guid businessCustomerId, CancellationToken cancellationToken);

    Task<bool> TryDeactivateAsync(Guid employeeId, DateTime utcNow, CancellationToken cancellationToken);
}
