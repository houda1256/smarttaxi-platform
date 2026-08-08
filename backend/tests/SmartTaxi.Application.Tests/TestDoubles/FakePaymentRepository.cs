using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePaymentRepository : IPaymentRepository
{
    private static readonly PaymentStatus[] TerminalStatuses = [PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Refunded];

    private readonly Dictionary<Guid, Payment> _paymentsById = new();

    public Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken)
    {
        var duplicateExists = _paymentsById.Values.Any(p => p.RideId == payment.RideId && !TerminalStatuses.Contains(p.Status));

        if (duplicateExists)
        {
            return Task.FromResult(false);
        }

        _paymentsById[payment.Id] = payment;
        return Task.FromResult(true);
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(_paymentsById.GetValueOrDefault(paymentId));

    public Task<Payment?> GetActiveByRideIdAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var payment = _paymentsById.Values.FirstOrDefault(p => p.RideId == rideId && !TerminalStatuses.Contains(p.Status));
        return Task.FromResult(payment);
    }

    public Task<IReadOnlyCollection<Payment>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Payment> payments = _paymentsById.Values.Where(p => p.CustomerId == customerId).ToList();
        return Task.FromResult(payments);
    }

    public Task<IReadOnlyCollection<Payment>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Payment> payments = _paymentsById.Values.Where(p => p.DriverId == driverId).ToList();
        return Task.FromResult(payments);
    }

    public Task<IReadOnlyCollection<Payment>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Payment> payments = _paymentsById.Values.Where(p => p.OwnerId == ownerId).ToList();
        return Task.FromResult(payments);
    }

    public Task<PagedResult<Payment>> SearchAsync(PaymentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _paymentsById.Values.AsEnumerable();

        if (filter.CustomerId is not null)
        {
            query = query.Where(p => p.CustomerId == filter.CustomerId);
        }

        if (filter.DriverId is not null)
        {
            query = query.Where(p => p.DriverId == filter.DriverId);
        }

        if (filter.OwnerId is not null)
        {
            query = query.Where(p => p.OwnerId == filter.OwnerId);
        }

        if (filter.Status is not null)
        {
            query = query.Where(p => p.Status == filter.Status);
        }

        if (filter.FromDate is not null)
        {
            query = query.Where(p => p.CreatedAt >= filter.FromDate);
        }

        if (filter.ToDate is not null)
        {
            query = query.Where(p => p.CreatedAt <= filter.ToDate);
        }

        var all = query.OrderByDescending(p => p.CreatedAt).ToList();
        var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<Payment>(page, all.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<Payment>> GetConfirmedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Payment> payments = _paymentsById.Values
            .Where(p => p.ConfirmedAt is not null && p.ConfirmedAt >= fromUtc && p.ConfirmedAt <= toUtc)
            .ToList();
        return Task.FromResult(payments);
    }

    public Task<IReadOnlyCollection<Payment>> GetCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Payment> payments = _paymentsById.Values
            .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt <= toUtc)
            .ToList();
        return Task.FromResult(payments);
    }

    public Task<bool> TryAuthorizeAsync(Guid paymentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_paymentsById.TryGetValue(paymentId, out var payment) || payment.Status != PaymentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(payment, nameof(Payment.Status), PaymentStatus.Authorized);
        SetProperty(payment, nameof(Payment.AuthorizedAt), utcNow);
        SetProperty(payment, nameof(Payment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryConfirmAsync(
        Guid paymentId, decimal platformCommissionAmount, decimal driverAmount, decimal ownerAmount, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!_paymentsById.TryGetValue(paymentId, out var payment)
            || payment.Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return Task.FromResult(false);
        }

        SetProperty(payment, nameof(Payment.Status), PaymentStatus.Paid);
        SetProperty(payment, nameof(Payment.PlatformCommissionAmount), platformCommissionAmount);
        SetProperty(payment, nameof(Payment.DriverAmount), driverAmount);
        SetProperty(payment, nameof(Payment.OwnerAmount), ownerAmount);
        SetProperty(payment, nameof(Payment.ConfirmedAt), utcNow);
        SetProperty(payment, nameof(Payment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryFailAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_paymentsById.TryGetValue(paymentId, out var payment)
            || payment.Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return Task.FromResult(false);
        }

        SetProperty(payment, nameof(Payment.Status), PaymentStatus.Failed);
        SetProperty(payment, nameof(Payment.FailedAt), utcNow);
        SetProperty(payment, nameof(Payment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryCancelAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_paymentsById.TryGetValue(paymentId, out var payment)
            || payment.Status is not (PaymentStatus.Pending or PaymentStatus.Authorized))
        {
            return Task.FromResult(false);
        }

        SetProperty(payment, nameof(Payment.Status), PaymentStatus.Cancelled);
        SetProperty(payment, nameof(Payment.CancelledAt), utcNow);
        SetProperty(payment, nameof(Payment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryApplyRefundAsync(
        Guid paymentId, decimal refundAmount, PaymentStatus newStatus, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_paymentsById.TryGetValue(paymentId, out var payment)
            || payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
        {
            return Task.FromResult(false);
        }

        var newRefundedAmount = payment.RefundedAmount + refundAmount;

        if (newRefundedAmount > payment.FinalFareAmount)
        {
            return Task.FromResult(false);
        }

        SetProperty(payment, nameof(Payment.RefundedAmount), newRefundedAmount);
        SetProperty(payment, nameof(Payment.Status), newStatus);
        SetProperty(payment, nameof(Payment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(Payment payment, string propertyName, object? value) =>
        typeof(Payment).GetProperty(propertyName)!.SetValue(payment, value);
}
