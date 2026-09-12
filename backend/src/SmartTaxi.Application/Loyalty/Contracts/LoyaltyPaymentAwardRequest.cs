using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Loyalty.Contracts;

/// <summary>
/// What Payments hands to ILoyaltyEarningDispatcher — the single seam Payments
/// calls, mirroring INotificationDispatcher's own contract shape. Amount comes
/// from the confirmed Payment itself (never trusted from a client), so Loyalty
/// never recomputes or trusts a frontend-supplied figure.
/// </summary>
public sealed record LoyaltyPaymentAwardRequest(Guid PayerUserId, UserRole PayerRole, Guid PaymentId, decimal Amount);
