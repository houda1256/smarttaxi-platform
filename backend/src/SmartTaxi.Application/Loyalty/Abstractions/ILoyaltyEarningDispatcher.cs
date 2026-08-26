using SmartTaxi.Application.Loyalty.Contracts;

namespace SmartTaxi.Application.Loyalty.Abstractions;

/// <summary>
/// The single seam Payments calls at the one safe earning trigger (Payment
/// confirmation — never Ride completion, see the Module 7 audit). Business
/// handlers depend only on this interface, never on any Loyalty repository or
/// EF entity directly. Never throws: a failure to award loyalty points must
/// never fail the payment confirmation that triggered it.
/// </summary>
public interface ILoyaltyEarningDispatcher
{
    Task AwardForPaymentAsync(LoyaltyPaymentAwardRequest request, CancellationToken cancellationToken);
}
