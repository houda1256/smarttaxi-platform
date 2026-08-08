using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Commands.FailPayment;

/// <summary>Admin/system-only — no ownership check, gated purely by the payments.manage permission at the API layer, same convention as CancelRideByAdmin.</summary>
public sealed record FailPaymentCommand(Guid PaymentId, string? Reason) : ICommand<Result>;
