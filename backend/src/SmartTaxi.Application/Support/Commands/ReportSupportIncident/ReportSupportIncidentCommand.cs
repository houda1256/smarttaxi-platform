using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.ReportSupportIncident;

/// <summary>Manual admin-authored incident report — no originating module event, so SourceType/SourceId stay null (no idempotency anchor needed: a human decides each time whether to file a new report).</summary>
public sealed record ReportSupportIncidentCommand(
    Guid ReportedByUserId, SupportIncidentType Type, SupportIncidentSeverity Severity, string Title, string Description,
    SupportRelatedEntityType? RelatedEntityType, Guid? RelatedEntityId, double? Latitude, double? Longitude) : ICommand<Result<Guid>>;
