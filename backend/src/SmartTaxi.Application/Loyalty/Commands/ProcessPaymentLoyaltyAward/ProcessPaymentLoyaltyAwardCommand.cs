using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessPaymentLoyaltyAward;

/// <summary>
/// The one safe earning trigger — called only from Payment confirmation, never
/// from Ride completion (see the Module 7 audit: a ride reaches AwaitingPayment
/// on completion and only reaches Completed once payment is actually
/// confirmed). Amount is whatever the caller passes; ILoyaltyEarningDispatcher
/// (the real seam Payments calls) always sources it from the confirmed
/// Payment's own FinalFareAmount, never a client-supplied value.
/// </summary>
public sealed record ProcessPaymentLoyaltyAwardCommand(Guid PayerUserId, UserRole PayerRole, Guid PaymentId, decimal Amount) : ICommand<Result>;
