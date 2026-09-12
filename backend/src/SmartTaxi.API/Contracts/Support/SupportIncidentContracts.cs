using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.API.Contracts.Support;

public sealed record ReportSupportIncidentRequest(
    SupportIncidentType Type, SupportIncidentSeverity Severity, string Title, string Description,
    SupportRelatedEntityType? RelatedEntityType, Guid? RelatedEntityId, double? Latitude, double? Longitude);

public sealed record CreateIncidentFromRideRequest(SupportIncidentType Type, SupportIncidentSeverity Severity, string? DescriptionOverride);

public sealed record CreateIncidentFromFinancialDisputeRequest(SupportIncidentSeverity Severity);

public sealed record CreateIncidentFromRoadsideRequest(SupportIncidentSeverity Severity);

public sealed record CreateIncidentFromMaintenanceRequest(SupportIncidentSeverity Severity);

public sealed record ReassignSupportIncidentRequest(Guid NewAdminUserId);

public sealed record ResolveSupportIncidentRequest(string Resolution);

public sealed record SupportIncidentResponse(
    Guid Id, string IncidentNumber, string Type, string Severity, string Title, string Description, Guid? ReportedByUserId,
    string? RelatedEntityType, Guid? RelatedEntityId, double? Latitude, double? Longitude, DateTime OccurredAtUtc, string Status,
    Guid? AssignedAdminUserId, string? Resolution, string? SourceType, Guid? SourceId, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    DateTime? ResolvedAtUtc, DateTime? ClosedAtUtc)
{
    public static SupportIncidentResponse FromEntity(SupportIncident incident) => new(
        incident.Id, incident.IncidentNumber, incident.Type.ToString(), incident.Severity.ToString(), incident.Title,
        incident.Description, incident.ReportedByUserId, incident.RelatedEntityType?.ToString(), incident.RelatedEntityId,
        incident.Latitude, incident.Longitude, incident.OccurredAtUtc, incident.Status.ToString(), incident.AssignedAdminUserId,
        incident.Resolution, incident.SourceType, incident.SourceId, incident.CreatedAtUtc, incident.UpdatedAtUtc,
        incident.ResolvedAtUtc, incident.ClosedAtUtc);
}
