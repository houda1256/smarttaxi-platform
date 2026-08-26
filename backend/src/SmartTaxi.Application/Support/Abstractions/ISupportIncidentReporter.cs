using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Abstractions;

/// <summary>
/// The single seam any caller uses to create a SupportIncident — mirrors
/// ILoyaltyEarningDispatcher's convention exactly: a plain, EF-free
/// Application-layer contract, so a future caller depends only on this
/// interface, never on Support's repositories or EF entities. In this pass
/// the only callers are Module 11's own command handlers (ticket escalation,
/// and the manual admin "create incident from X" commands) — no other
/// module's source code takes a dependency on it.
/// </summary>
public interface ISupportIncidentReporter
{
    /// <summary>Idempotent: retrying with the same (SourceType, SourceId) returns the existing incident's id, never creates a duplicate.</summary>
    Task<Guid> ReportAsync(SupportIncidentReportRequest request, CancellationToken cancellationToken);
}

public sealed record SupportIncidentReportRequest(
    SupportIncidentType Type, SupportIncidentSeverity Severity, string Title, string Description, Guid? ReportedByUserId,
    SupportRelatedEntityType? RelatedEntityType, Guid? RelatedEntityId, double? Latitude, double? Longitude, DateTime OccurredAtUtc,
    string? SourceType, Guid? SourceId);
