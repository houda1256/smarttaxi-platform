using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Subscriptions.Repositories;

internal sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        await _context.SubscriptionPlans.AddAsync(plan, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The unique index on Code rejected a concurrent duplicate plan.
            _context.Entry(plan).State = EntityState.Detached;
            return false;
        }
    }

    public Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken) =>
        _context.SubscriptionPlans.FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken);

    public Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.SubscriptionPlans.FirstOrDefaultAsync(plan => plan.Code == code.Trim().ToUpper(), cancellationToken);

    public Task UpdateAsync(SubscriptionPlan plan, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<PagedResult<SubscriptionPlan>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.SubscriptionPlans.OrderBy(plan => plan.Name);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<SubscriptionPlan>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<SubscriptionPlan>> GetActiveByRoleAsync(
        UserRole targetRole, CancellationToken cancellationToken) =>
        await _context.SubscriptionPlans
            .Where(plan => plan.TargetRole == targetRole && plan.IsActive)
            .OrderBy(plan => plan.Price)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryActivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SubscriptionPlans
            .Where(plan => plan.Id == planId && !plan.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(plan => plan.IsActive, true)
                .SetProperty(plan => plan.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryDeactivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SubscriptionPlans
            .Where(plan => plan.Id == planId && plan.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(plan => plan.IsActive, false)
                .SetProperty(plan => plan.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
