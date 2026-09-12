using SmartTaxi.Application.Fleet.Fleets;

namespace SmartTaxi.API.Contracts.Fleet.Fleets;

public sealed record FleetMemberResponse(Guid Id, Guid FleetId, Guid UserId, string Role, DateTime CreatedAt)
{
    public static FleetMemberResponse FromSummary(FleetMemberSummary summary) => new(
        summary.Id, summary.FleetId, summary.UserId, summary.Role.ToString(), summary.CreatedAt);
}
