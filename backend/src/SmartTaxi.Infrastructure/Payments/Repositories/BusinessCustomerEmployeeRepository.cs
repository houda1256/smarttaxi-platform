using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class BusinessCustomerEmployeeRepository : IBusinessCustomerEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public BusinessCustomerEmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BusinessCustomerEmployee employee, CancellationToken cancellationToken)
    {
        await _context.BusinessCustomerEmployees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<BusinessCustomerEmployee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        _context.BusinessCustomerEmployees.FirstOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);

    public Task<BusinessCustomerEmployee?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.BusinessCustomerEmployees.FirstOrDefaultAsync(employee => employee.UserId == userId && employee.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<BusinessCustomerEmployee>> GetForBusinessCustomerAsync(Guid businessCustomerId, CancellationToken cancellationToken) =>
        await _context.BusinessCustomerEmployees
            .Where(employee => employee.BusinessCustomerId == businessCustomerId)
            .OrderBy(employee => employee.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryDeactivateAsync(Guid employeeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomerEmployees
            .Where(employee => employee.Id == employeeId && employee.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(employee => employee.IsActive, false), cancellationToken);

        return rows == 1;
    }
}
