using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Fleet.Fleets;

public sealed record FleetMemberSummary(Guid Id, Guid FleetId, Guid UserId, FleetCollaboratorRole Role, DateTime CreatedAt)
{
    public static FleetMemberSummary FromEntity(FleetMember member) => new(
        member.Id, member.FleetId, member.UserId, member.Role, member.CreatedAt);
}
