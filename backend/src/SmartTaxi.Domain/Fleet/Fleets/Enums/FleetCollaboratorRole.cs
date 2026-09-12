namespace SmartTaxi.Domain.Fleet.Fleets.Enums;

/// <summary>
/// Organization-scoped collaborator role, restricted to the fleet(s) the
/// membership grants access to — never to be confused with a platform-level
/// role in Identity.UserRole.
/// </summary>
public enum FleetCollaboratorRole
{
    FleetManager,
    Accountant,
    MaintenanceManager,
    Dispatcher,
    Viewer
}
