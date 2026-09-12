using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeBusinessCustomerEmployeeRepository : IBusinessCustomerEmployeeRepository
{
    private readonly Dictionary<Guid, BusinessCustomerEmployee> _employeesById = new();

    public Task AddAsync(BusinessCustomerEmployee employee, CancellationToken cancellationToken)
    {
        _employeesById[employee.Id] = employee;
        return Task.CompletedTask;
    }

    public Task<BusinessCustomerEmployee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(_employeesById.GetValueOrDefault(employeeId));

    public Task<BusinessCustomerEmployee?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var employee = _employeesById.Values.FirstOrDefault(e => e.UserId == userId && e.IsActive);
        return Task.FromResult(employee);
    }

    public Task<IReadOnlyCollection<BusinessCustomerEmployee>> GetForBusinessCustomerAsync(Guid businessCustomerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BusinessCustomerEmployee> employees = _employeesById.Values.Where(e => e.BusinessCustomerId == businessCustomerId).ToList();
        return Task.FromResult(employees);
    }

    public Task<bool> TryDeactivateAsync(Guid employeeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_employeesById.TryGetValue(employeeId, out var employee) || !employee.IsActive)
        {
            return Task.FromResult(false);
        }

        typeof(BusinessCustomerEmployee).GetProperty(nameof(BusinessCustomerEmployee.IsActive))!.SetValue(employee, false);
        return Task.FromResult(true);
    }
}
