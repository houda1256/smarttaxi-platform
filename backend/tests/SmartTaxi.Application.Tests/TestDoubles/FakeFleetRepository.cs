using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFleetRepository : IFleetRepository
{
    private readonly Dictionary<Guid, FleetOrganization> _fleetsById = new();

    public Task AddAsync(FleetOrganization fleet, CancellationToken cancellationToken)
    {
        _fleetsById[fleet.Id] = fleet;
        return Task.CompletedTask;
    }

    public Task<FleetOrganization?> GetByIdAsync(Guid fleetId, CancellationToken cancellationToken) =>
        Task.FromResult(_fleetsById.GetValueOrDefault(fleetId));

    public Task<IReadOnlyCollection<FleetOrganization>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FleetOrganization> fleets = _fleetsById.Values.Where(f => f.OwnerId == ownerId).ToList();
        return Task.FromResult(fleets);
    }

    public Task UpdateAsync(FleetOrganization fleet, CancellationToken cancellationToken)
    {
        _fleetsById[fleet.Id] = fleet;
        return Task.CompletedTask;
    }

    public Task<bool> TrySuspendAsync(Guid fleetId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_fleetsById.TryGetValue(fleetId, out var fleet) || fleet.Status != FleetStatus.Active)
        {
            return Task.FromResult(false);
        }

        typeof(FleetOrganization).GetProperty(nameof(FleetOrganization.Status))!.SetValue(fleet, FleetStatus.Suspended);
        typeof(FleetOrganization).GetProperty(nameof(FleetOrganization.UpdatedAt))!.SetValue(fleet, utcNow);
        return Task.FromResult(true);
    }
}
