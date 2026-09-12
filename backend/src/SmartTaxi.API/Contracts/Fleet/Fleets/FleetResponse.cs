using SmartTaxi.Application.Fleet.Fleets;

namespace SmartTaxi.API.Contracts.Fleet.Fleets;

public sealed record FleetResponse(
    Guid Id, Guid OwnerId, string Name, string? Description, Guid CityId, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static FleetResponse FromSummary(FleetSummary summary) => new(
        summary.Id, summary.OwnerId, summary.Name, summary.Description, summary.CityId, summary.Status.ToString(),
        summary.CreatedAt, summary.UpdatedAt);
}
