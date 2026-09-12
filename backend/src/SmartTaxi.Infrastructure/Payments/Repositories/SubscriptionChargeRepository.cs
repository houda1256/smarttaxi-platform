using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class SubscriptionChargeRepository : ISubscriptionChargeRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionChargeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SubscriptionCharge charge, CancellationToken cancellationToken)
    {
        await _context.SubscriptionCharges.AddAsync(charge, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<SubscriptionCharge?> GetByIdAsync(Guid chargeId, CancellationToken cancellationToken) =>
        _context.SubscriptionCharges.FirstOrDefaultAsync(charge => charge.Id == chargeId, cancellationToken);

    public async Task<IReadOnlyCollection<SubscriptionCharge>> GetForSubscriptionAsync(
        Guid subscriptionId, CancellationToken cancellationToken) =>
        await _context.SubscriptionCharges
            .Where(charge => charge.SubscriptionId == subscriptionId)
            .OrderByDescending(charge => charge.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryConfirmAsync(Guid chargeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SubscriptionCharges
            .Where(charge => charge.Id == chargeId && charge.Status == SubscriptionChargeStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(charge => charge.Status, SubscriptionChargeStatus.Confirmed)
                .SetProperty(charge => charge.ConfirmedAt, utcNow)
                .SetProperty(charge => charge.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryFailAsync(Guid chargeId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SubscriptionCharges
            .Where(charge => charge.Id == chargeId && charge.Status == SubscriptionChargeStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(charge => charge.Status, SubscriptionChargeStatus.Failed)
                .SetProperty(charge => charge.FailureReason, reason)
                .SetProperty(charge => charge.FailedAt, utcNow)
                .SetProperty(charge => charge.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
