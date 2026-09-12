using SmartTaxi.Application.Fleet.Alerts;

namespace SmartTaxi.API.Contracts.Fleet.Alerts;

public sealed record FleetAlertResponse(
    Guid Id, Guid OwnerId, Guid? FleetId, string AlertType, Guid? RelatedEntityId, string Message, string Status,
    DateTime CreatedAt, DateTime? ResolvedAt)
{
    public static FleetAlertResponse FromSummary(FleetAlertSummary summary) => new(
        summary.Id, summary.OwnerId, summary.FleetId, summary.AlertType.ToString(), summary.RelatedEntityId,
        summary.Message, summary.Status.ToString(), summary.CreatedAt, summary.ResolvedAt);
}
