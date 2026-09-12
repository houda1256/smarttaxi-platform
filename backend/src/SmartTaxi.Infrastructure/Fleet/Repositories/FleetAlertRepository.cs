using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Alerts.Abstractions;
using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class FleetAlertRepository : IFleetAlertRepository
{
    private readonly ApplicationDbContext _context;

    public FleetAlertRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FleetAlert alert, CancellationToken cancellationToken)
    {
        await _context.FleetAlerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<FleetAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken) =>
        _context.FleetAlerts.FirstOrDefaultAsync(alert => alert.Id == alertId, cancellationToken);

    public async Task<IReadOnlyCollection<FleetAlert>> GetForOwnerAsync(
        Guid ownerId, FleetAlertStatus? status, CancellationToken cancellationToken)
    {
        var query = _context.FleetAlerts.Where(alert => alert.OwnerId == ownerId);

        if (status is not null)
        {
            query = query.Where(alert => alert.Status == status);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsOpenAlertAsync(
        Guid ownerId, FleetAlertType alertType, Guid? relatedEntityId, CancellationToken cancellationToken) =>
        _context.FleetAlerts.AnyAsync(
            alert => alert.OwnerId == ownerId && alert.AlertType == alertType && alert.RelatedEntityId == relatedEntityId
                && alert.Status == FleetAlertStatus.Open, cancellationToken);

    public Task<bool> TryResolveAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(alertId, FleetAlertStatus.Resolved, utcNow, cancellationToken);

    public Task<bool> TryDismissAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(alertId, FleetAlertStatus.Dismissed, utcNow, cancellationToken);

    private async Task<bool> TryTransitionAsync(
        Guid alertId, FleetAlertStatus to, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.FleetAlerts
            .Where(alert => alert.Id == alertId && alert.Status == FleetAlertStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(alert => alert.Status, to)
                .SetProperty(alert => alert.ResolvedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
