using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Fleet.Fleets.Abstractions;

public interface IFleetRepository
{
    Task AddAsync(FleetOrganization fleet, CancellationToken cancellationToken);

    Task<FleetOrganization?> GetByIdAsync(Guid fleetId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FleetOrganization>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task UpdateAsync(FleetOrganization fleet, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid fleetId, DateTime utcNow, CancellationToken cancellationToken);
}
