using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Commands.RefundPayment;

/// <summary>Admin-only (payments.refund) — Amount is ignored for Full/Cancellation (always the full remaining refundable amount) and required for Partial.</summary>
public sealed record RefundPaymentCommand(Guid AdminUserId, Guid PaymentId, RefundType RefundType, decimal? Amount, string Reason)
    : ICommand<Result<Guid>>;
