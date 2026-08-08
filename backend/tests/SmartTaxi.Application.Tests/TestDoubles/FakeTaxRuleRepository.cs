using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTaxRuleRepository : ITaxRuleRepository
{
    private readonly Dictionary<Guid, TaxRule> _rulesById = new();

    public Task AddAsync(TaxRule rule, CancellationToken cancellationToken)
    {
        _rulesById[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task<TaxRule?> GetByIdAsync(Guid taxRuleId, CancellationToken cancellationToken) =>
        Task.FromResult(_rulesById.GetValueOrDefault(taxRuleId));

    public Task<TaxRule?> GetApplicableRuleAsync(string applicableService, DateOnly date, CancellationToken cancellationToken)
    {
        var rule = _rulesById.Values
            .Where(r => r.IsApplicableOn(date, applicableService))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();
        return Task.FromResult(rule);
    }

    public Task<PagedResult<TaxRule>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _rulesById.Values.ToList();
        return Task.FromResult(new PagedResult<TaxRule>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TryDeactivateAsync(Guid taxRuleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_rulesById.TryGetValue(taxRuleId, out var rule) || !rule.IsActive)
        {
            return Task.FromResult(false);
        }

        typeof(TaxRule).GetProperty(nameof(TaxRule.IsActive))!.SetValue(rule, false);
        typeof(TaxRule).GetProperty(nameof(TaxRule.UpdatedAt))!.SetValue(rule, utcNow);
        return Task.FromResult(true);
    }
}
