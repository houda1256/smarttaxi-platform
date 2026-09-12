using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record CreateRidePaymentRequest(Guid RideId, PaymentMethod PaymentMethod);

public sealed record CancelPaymentRequest(string? Reason);

public sealed record FailPaymentRequest(string? Reason);

public sealed record RefundPaymentRequest(RefundType RefundType, decimal? Amount, string Reason);
