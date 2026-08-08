using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.Taxes.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class TaxRuleRepository : ITaxRuleRepository
{
    private readonly ApplicationDbContext _context;

    public TaxRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaxRule rule, CancellationToken cancellationToken)
    {
        await _context.TaxRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<TaxRule?> GetByIdAsync(Guid taxRuleId, CancellationToken cancellationToken) =>
        _context.TaxRules.FirstOrDefaultAsync(rule => rule.Id == taxRuleId, cancellationToken);

    public Task<TaxRule?> GetApplicableRuleAsync(string applicableService, DateOnly date, CancellationToken cancellationToken) =>
        _context.TaxRules
            .Where(rule => rule.IsActive
                && rule.EffectiveFrom <= date
                && (rule.EffectiveTo == null || rule.EffectiveTo >= date)
                && (rule.ApplicableService == "All" || rule.ApplicableService == applicableService))
            .OrderByDescending(rule => rule.EffectiveFrom)
            .ThenByDescending(rule => rule.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<TaxRule>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.TaxRules.CountAsync(cancellationToken);
        var items = await _context.TaxRules
            .OrderByDescending(rule => rule.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaxRule>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TryDeactivateAsync(Guid taxRuleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.TaxRules
            .Where(rule => rule.Id == taxRuleId && rule.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rule => rule.IsActive, false)
                .SetProperty(rule => rule.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
