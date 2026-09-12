using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class BusinessCustomerRepository : IBusinessCustomerRepository
{
    private readonly ApplicationDbContext _context;

    public BusinessCustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken)
    {
        await _context.BusinessCustomers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<BusinessCustomer?> GetByIdAsync(Guid businessCustomerId, CancellationToken cancellationToken) =>
        _context.BusinessCustomers.FirstOrDefaultAsync(customer => customer.Id == businessCustomerId, cancellationToken);

    public async Task<PagedResult<BusinessCustomer>> GetAllAsync(
        BusinessCustomerStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.BusinessCustomers.AsQueryable();

        if (status is not null)
        {
            query = query.Where(customer => customer.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(customer => customer.LegalName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BusinessCustomer>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TrySuspendAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomers
            .Where(customer => customer.Id == businessCustomerId && customer.Status == BusinessCustomerStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.Status, BusinessCustomerStatus.Suspended)
                .SetProperty(customer => customer.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryReactivateAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomers
            .Where(customer => customer.Id == businessCustomerId && customer.Status == BusinessCustomerStatus.Suspended)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.Status, BusinessCustomerStatus.Active)
                .SetProperty(customer => customer.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryCloseAsync(Guid businessCustomerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomers
            .Where(customer => customer.Id == businessCustomerId && customer.Status != BusinessCustomerStatus.Closed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.Status, BusinessCustomerStatus.Closed)
                .SetProperty(customer => customer.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryIncreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomers
            .Where(customer => customer.Id == businessCustomerId && customer.CurrentCreditUsage + amount <= customer.CreditLimit)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.CurrentCreditUsage, customer => customer.CurrentCreditUsage + amount)
                .SetProperty(customer => customer.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryDecreaseCreditUsageAsync(Guid businessCustomerId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.BusinessCustomers
            .Where(customer => customer.Id == businessCustomerId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(customer => customer.CurrentCreditUsage, customer => customer.CurrentCreditUsage - amount > 0 ? customer.CurrentCreditUsage - amount : 0m)
                .SetProperty(customer => customer.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
