using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyEarningRuleRepository
{
    Task<LoyaltyEarningRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken);

    Task<LoyaltyEarningRule?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>The one active, effective rule for a given actor role and source — callers apply .IsEffective(utcNow) filtering is already done here.</summary>
    Task<LoyaltyEarningRule?> GetEffectiveAsync(UserRole actorRole, string sourceType, DateTime utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LoyaltyEarningRule>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken);
}
