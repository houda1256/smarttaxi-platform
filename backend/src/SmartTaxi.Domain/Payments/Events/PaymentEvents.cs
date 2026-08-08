using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Payments.Events;

/// <summary>Genuinely raised on creation (unit-testable). Every other event below is documentation-only, never raised — status transitions bypass entity mutation, same convention as Ride.</summary>
public sealed record PaymentCreated(Guid PaymentId, Guid RideId, Guid CustomerId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentAuthorized(Guid PaymentId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentConfirmed(Guid PaymentId, decimal Amount, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentFailed(Guid PaymentId, string? Reason, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentCancelled(Guid PaymentId, string? Reason, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RevenueDistributed(Guid PaymentId, decimal DriverAmount, decimal OwnerAmount, decimal PlatformCommission, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RefundIssued(Guid RefundRecordId, Guid PaymentId, decimal Amount, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record InvoiceGenerated(Guid InvoiceId, Guid PaymentId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record ReceiptGenerated(Guid ReceiptId, Guid PaymentId, DateTime OccurredAtUtc) : IDomainEvent;
