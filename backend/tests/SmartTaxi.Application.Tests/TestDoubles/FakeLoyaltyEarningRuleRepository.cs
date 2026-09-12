using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyEarningRuleRepository : ILoyaltyEarningRuleRepository
{
    private readonly Dictionary<Guid, LoyaltyEarningRule> _rules = new();

    public Task<LoyaltyEarningRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
        Task.FromResult(_rules.GetValueOrDefault(ruleId));

    public Task<LoyaltyEarningRule?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_rules.Values.FirstOrDefault(r => r.Code == code.Trim().ToUpper()));

    public Task<LoyaltyEarningRule?> GetEffectiveAsync(UserRole actorRole, string sourceType, DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult(_rules.Values.FirstOrDefault(r => r.ActorRole == actorRole && r.SourceType == sourceType && r.IsEffective(utcNow)));

    public Task<IReadOnlyCollection<LoyaltyEarningRule>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyEarningRule>>(_rules.Values.ToList());

    public Task AddAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyEarningRule rule, CancellationToken cancellationToken)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }
}
