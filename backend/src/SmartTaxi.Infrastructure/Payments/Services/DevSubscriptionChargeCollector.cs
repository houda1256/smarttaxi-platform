using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;

namespace SmartTaxi.Infrastructure.Payments.Services;

/// <summary>
/// Deterministic simulated payment gateway for subscription billing — no
/// real payment processor is integrated (consistent with the rest of
/// Payments: PaymentMethod is recorded but never actually calls out to a
/// bank/card network). Always confirms unless the amount/currency is
/// invalid (Money.Create's own validation), so it stays testable and never
/// silently no-ops. A real gateway-backed implementation is a drop-in
/// replacement behind ISubscriptionChargeCollector.
/// </summary>
internal sealed class DevSubscriptionChargeCollector : ISubscriptionChargeCollector
{
    private readonly ISubscriptionChargeRepository _chargeRepository;

    public DevSubscriptionChargeCollector(ISubscriptionChargeRepository chargeRepository)
    {
        _chargeRepository = chargeRepository;
    }

    public async Task<Result<Guid>> ChargeAsync(
        Guid subscriberId, Guid subscriptionId, decimal amount, string currency, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        SubscriptionCharge charge;

        try
        {
            charge = SubscriptionCharge.Create(subscriberId, subscriptionId, amount, currency, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _chargeRepository.AddAsync(charge, cancellationToken);
        await _chargeRepository.TryConfirmAsync(charge.Id, utcNow, cancellationToken);

        return Result<Guid>.Success(charge.Id);
    }
}
