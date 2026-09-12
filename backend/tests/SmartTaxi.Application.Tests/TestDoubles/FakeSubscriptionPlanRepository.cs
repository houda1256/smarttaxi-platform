using SmartTaxi.Application.Common;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly Dictionary<Guid, SubscriptionPlan> _plansById = new();

    public Task<bool> TryAddAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
    {
        if (_plansById.Values.Any(p => p.Code == plan.Code))
        {
            return Task.FromResult(false);
        }

        _plansById[plan.Id] = plan;
        return Task.FromResult(true);
    }

    public Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken) =>
        Task.FromResult(_plansById.GetValueOrDefault(planId));

    public Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_plansById.Values.FirstOrDefault(p => p.Code == code.Trim().ToUpperInvariant()));

    public Task UpdateAsync(SubscriptionPlan plan, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PagedResult<SubscriptionPlan>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _plansById.Values.ToList();
        return Task.FromResult(new PagedResult<SubscriptionPlan>(items, items.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<SubscriptionPlan>> GetActiveByRoleAsync(UserRole targetRole, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<SubscriptionPlan>>(
            _plansById.Values.Where(p => p.TargetRole == targetRole && p.IsActive).ToList());

    public Task<bool> TryActivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_plansById.TryGetValue(planId, out var plan) || plan.IsActive)
        {
            return Task.FromResult(false);
        }

        SetProperty(plan, nameof(SubscriptionPlan.IsActive), true);
        return Task.FromResult(true);
    }

    public Task<bool> TryDeactivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_plansById.TryGetValue(planId, out var plan) || !plan.IsActive)
        {
            return Task.FromResult(false);
        }

        SetProperty(plan, nameof(SubscriptionPlan.IsActive), false);
        return Task.FromResult(true);
    }

    private static void SetProperty(SubscriptionPlan plan, string propertyName, object? value) =>
        typeof(SubscriptionPlan).GetProperty(propertyName)!.SetValue(plan, value);
}
