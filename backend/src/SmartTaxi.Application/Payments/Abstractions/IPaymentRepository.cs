using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Abstractions;

public interface IPaymentRepository
{
    /// <summary>May return false if a non-terminal Payment already exists for this Ride — see the DB partial unique index on RideId (the "no duplicate payment" guarantee).</summary>
    Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken);

    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>The current non-terminal Payment for a Ride, if any — used both for duplicate-prevention and to look a Payment up by its Ride.</summary>
    Task<Payment?> GetActiveByRideIdAsync(Guid rideId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Payment>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Payment>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Payment>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<PagedResult<Payment>> SearchAsync(PaymentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Payments confirmed (ConfirmedAt set) within the window — the read-model source for every revenue report.</summary>
    Task<IReadOnlyCollection<Payment>> GetConfirmedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    /// <summary>All Payments created within the window, any status — used for payment statistics (counts by status).</summary>
    Task<IReadOnlyCollection<Payment>> GetCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<bool> TryAuthorizeAsync(Guid paymentId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Guarded by Status IN (Pending, Authorized) — this is what makes payment
    /// confirmation idempotent: a second concurrent/retried call always fails
    /// here once the first has landed, and the handler treats "already Paid"
    /// as a success no-op rather than an error.
    /// </summary>
    Task<bool> TryConfirmAsync(
        Guid paymentId, decimal platformCommissionAmount, decimal driverAmount, decimal ownerAmount, DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> TryFailAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded by Status IN (Paid, PartiallyRefunded) and re-verifies the cumulative refunded amount does not exceed the final fare, atomically.</summary>
    Task<bool> TryApplyRefundAsync(
        Guid paymentId, decimal refundAmount, PaymentStatus newStatus, DateTime utcNow, CancellationToken cancellationToken);
}

public sealed record PaymentFilter(
    Guid? CustomerId = null,
    Guid? DriverId = null,
    Guid? OwnerId = null,
    PaymentStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null);
