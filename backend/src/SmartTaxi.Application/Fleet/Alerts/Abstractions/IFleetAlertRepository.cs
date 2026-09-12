using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Application.Fleet.Alerts.Abstractions;

public interface IFleetAlertRepository
{
    Task AddAsync(FleetAlert alert, CancellationToken cancellationToken);

    Task<FleetAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FleetAlert>> GetForOwnerAsync(
        Guid ownerId, FleetAlertStatus? status, CancellationToken cancellationToken);

    /// <summary>Used to avoid re-raising a duplicate alert for the same condition while one is still open.</summary>
    Task<bool> ExistsOpenAlertAsync(
        Guid ownerId, FleetAlertType alertType, Guid? relatedEntityId, CancellationToken cancellationToken);

    Task<bool> TryResolveAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDismissAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken);
}
