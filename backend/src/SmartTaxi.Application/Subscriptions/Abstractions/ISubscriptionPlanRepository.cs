using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Abstractions;

public interface ISubscriptionPlanRepository
{
    /// <summary>May return false if a plan with this Code already exists — see the DB unique index on Code.</summary>
    Task<bool> TryAddAsync(SubscriptionPlan plan, CancellationToken cancellationToken);

    Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken);

    Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task UpdateAsync(SubscriptionPlan plan, CancellationToken cancellationToken);

    Task<PagedResult<SubscriptionPlan>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>The self-service catalog for a given role — active plans only.</summary>
    Task<IReadOnlyCollection<SubscriptionPlan>> GetActiveByRoleAsync(UserRole targetRole, CancellationToken cancellationToken);

    Task<bool> TryActivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDeactivateAsync(Guid planId, DateTime utcNow, CancellationToken cancellationToken);
}
