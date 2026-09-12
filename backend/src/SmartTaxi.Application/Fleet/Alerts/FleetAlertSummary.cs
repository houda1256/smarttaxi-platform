using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Application.Fleet.Alerts;

public sealed record FleetAlertSummary(
    Guid Id, Guid OwnerId, Guid? FleetId, FleetAlertType AlertType, Guid? RelatedEntityId, string Message,
    FleetAlertStatus Status, DateTime CreatedAt, DateTime? ResolvedAt)
{
    public static FleetAlertSummary FromEntity(FleetAlert alert) => new(
        alert.Id, alert.OwnerId, alert.FleetId, alert.AlertType, alert.RelatedEntityId, alert.Message,
        alert.Status, alert.CreatedAt, alert.ResolvedAt);
}
