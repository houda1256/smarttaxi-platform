using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyEarningRuleRepository : ILoyaltyEarningRuleRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyEarningRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyEarningRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
        _context.LoyaltyEarningRules.FirstOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    public Task<LoyaltyEarningRule?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.LoyaltyEarningRules.FirstOrDefaultAsync(rule => rule.Code == code.Trim().ToUpper(), cancellationToken);

    public async Task<LoyaltyEarningRule?> GetEffectiveAsync(
        UserRole actorRole, string sourceType, DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await _context.LoyaltyEarningRules
            .Where(rule => rule.ActorRole == actorRole && rule.SourceType == sourceType && rule.IsActive)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(rule => rule.IsEffective(utcNow));
    }

    public async Task<IReadOnlyCollection<LoyaltyEarningRule>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.LoyaltyEarningRules.OrderBy(rule => rule.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken)
    {
        await _context.LoyaltyEarningRules.AddAsync(rule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken)
    {
        _context.LoyaltyEarningRules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
