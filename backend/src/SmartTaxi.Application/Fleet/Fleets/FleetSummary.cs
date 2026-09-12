using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Fleet.Fleets;

public sealed record FleetSummary(
    Guid Id, Guid OwnerId, string Name, string? Description, Guid CityId, FleetStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    public static FleetSummary FromEntity(FleetOrganization fleet) => new(
        fleet.Id, fleet.OwnerId, fleet.Name, fleet.Description, fleet.CityId, fleet.Status, fleet.CreatedAt, fleet.UpdatedAt);
}
