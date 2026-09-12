using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Domain.Fleet.Fleets.Entities;

/// <summary>An internal collaborator's organization-scoped membership in one fleet.</summary>
public sealed class FleetMember
{
    public Guid Id { get; private set; }
    public Guid FleetId { get; private set; }
    public Guid UserId { get; private set; }
    public FleetCollaboratorRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FleetMember()
    {
    }

    public FleetMember(Guid fleetId, Guid userId, FleetCollaboratorRole role, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        FleetId = fleetId;
        UserId = userId;
        Role = role;
        CreatedAt = utcNow;
    }
}
