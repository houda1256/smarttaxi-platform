using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;

namespace SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;

public interface ISubscriptionChargeRepository
{
    Task AddAsync(SubscriptionCharge charge, CancellationToken cancellationToken);

    Task<SubscriptionCharge?> GetByIdAsync(Guid chargeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SubscriptionCharge>> GetForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);

    Task<bool> TryConfirmAsync(Guid chargeId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryFailAsync(Guid chargeId, string reason, DateTime utcNow, CancellationToken cancellationToken);
}
