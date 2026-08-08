using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Domain.Fleet.Alerts.Entities;

/// <summary>Domain/application-level alert only — no external monitoring or paging integration.</summary>
public sealed class FleetAlert
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid? FleetId { get; private set; }
    public FleetAlertType AlertType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public FleetAlertStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private FleetAlert()
    {
    }

    public FleetAlert(Guid ownerId, Guid? fleetId, FleetAlertType alertType, Guid? relatedEntityId, string message, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        FleetId = fleetId;
        AlertType = alertType;
        RelatedEntityId = relatedEntityId;
        Message = message;
        Status = FleetAlertStatus.Open;
        CreatedAt = utcNow;
    }
}
