using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

/// <summary>
/// TerminalStatuses mirrors PaymentStatusTransitions.IsTerminal (kept private
/// to the Domain project) so "duplicate active payment" queries stay
/// translatable to SQL — the same duplication pattern already used by
/// RideRepository for RideStatusTransitions.
/// </summary>
internal sealed class PaymentRepository : IPaymentRepository
{
    private static readonly PaymentStatus[] TerminalStatuses = [PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Refunded];
    private static readonly PaymentStatus[] LiveForConfirmStatuses = [PaymentStatus.Pending, PaymentStatus.Authorized];
    private static readonly PaymentStatus[] RefundableStatuses = [PaymentStatus.Paid, PaymentStatus.PartiallyRefunded];

    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The partial unique index on RideId (non-terminal statuses) rejected a concurrent duplicate payment.
            _context.Entry(payment).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        _context.Payments.FirstOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

    public Task<Payment?> GetActiveByRideIdAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.Payments.FirstOrDefaultAsync(
            payment => payment.RideId == rideId && !TerminalStatuses.Contains(payment.Status), cancellationToken);

    public async Task<IReadOnlyCollection<Payment>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(payment => payment.CustomerId == customerId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Payment>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(payment => payment.DriverId == driverId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Payment>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(payment => payment.OwnerId == ownerId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Payment>> SearchAsync(PaymentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Payments.AsQueryable();

        if (filter.CustomerId is not null)
        {
            query = query.Where(payment => payment.CustomerId == filter.CustomerId);
        }

        if (filter.DriverId is not null)
        {
            query = query.Where(payment => payment.DriverId == filter.DriverId);
        }

        if (filter.OwnerId is not null)
        {
            query = query.Where(payment => payment.OwnerId == filter.OwnerId);
        }

        if (filter.Status is not null)
        {
            query = query.Where(payment => payment.Status == filter.Status);
        }

        if (filter.FromDate is not null)
        {
            query = query.Where(payment => payment.CreatedAt >= filter.FromDate);
        }

        if (filter.ToDate is not null)
        {
            query = query.Where(payment => payment.CreatedAt <= filter.ToDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<Payment>> GetConfirmedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(payment => payment.ConfirmedAt != null && payment.ConfirmedAt >= fromUtc && payment.ConfirmedAt <= toUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Payment>> GetCreatedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await _context.Payments
            .Where(payment => payment.CreatedAt >= fromUtc && payment.CreatedAt <= toUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryAuthorizeAsync(Guid paymentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PaymentStatus.Authorized)
                .SetProperty(p => p.AuthorizedAt, utcNow)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.PaymentTransactionHistories.AddAsync(
            new PaymentTransactionHistory(paymentId, PaymentStatus.Pending, PaymentStatus.Authorized, null, null, null, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryConfirmAsync(
        Guid paymentId, decimal platformCommissionAmount, decimal driverAmount, decimal ownerAmount, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null || !LiveForConfirmStatuses.Contains(payment.Status))
        {
            return false;
        }

        var previousStatus = payment.Status;

        var rows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == previousStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PaymentStatus.Paid)
                .SetProperty(p => p.PlatformCommissionAmount, platformCommissionAmount)
                .SetProperty(p => p.DriverAmount, driverAmount)
                .SetProperty(p => p.OwnerAmount, ownerAmount)
                .SetProperty(p => p.ConfirmedAt, utcNow)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.PaymentTransactionHistories.AddAsync(
            new PaymentTransactionHistory(paymentId, previousStatus, PaymentStatus.Paid, null, null, driverAmount + ownerAmount + platformCommissionAmount, utcNow),
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryFailAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null || !LiveForConfirmStatuses.Contains(payment.Status))
        {
            return false;
        }

        var previousStatus = payment.Status;

        var rows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == previousStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PaymentStatus.Failed)
                .SetProperty(p => p.FailedAt, utcNow)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.PaymentTransactionHistories.AddAsync(
            new PaymentTransactionHistory(paymentId, previousStatus, PaymentStatus.Failed, null, reason, null, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryCancelAsync(Guid paymentId, string? reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null || !LiveForConfirmStatuses.Contains(payment.Status))
        {
            return false;
        }

        var previousStatus = payment.Status;

        var rows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == previousStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PaymentStatus.Cancelled)
                .SetProperty(p => p.CancelledAt, utcNow)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.PaymentTransactionHistories.AddAsync(
            new PaymentTransactionHistory(paymentId, previousStatus, PaymentStatus.Cancelled, null, reason, null, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryApplyRefundAsync(
        Guid paymentId, decimal refundAmount, PaymentStatus newStatus, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null || !RefundableStatuses.Contains(payment.Status))
        {
            return false;
        }

        var newRefundedAmount = payment.RefundedAmount + refundAmount;

        if (newRefundedAmount > payment.FinalFareAmount)
        {
            return false;
        }

        var previousStatus = payment.Status;

        // Re-checks both Status and RefundedAmount atomically against the DB row, so two concurrent
        // partial refunds can never together push the cumulative amount past the final fare.
        var rows = await _context.Payments
            .Where(p => p.Id == paymentId && p.Status == previousStatus && p.RefundedAmount == payment.RefundedAmount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.RefundedAmount, newRefundedAmount)
                .SetProperty(p => p.Status, newStatus)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await _context.PaymentTransactionHistories.AddAsync(
            new PaymentTransactionHistory(paymentId, previousStatus, newStatus, null, null, refundAmount, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
