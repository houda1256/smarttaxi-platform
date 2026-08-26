using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Domain.Support.Events;

namespace SmartTaxi.Domain.Support.Entities;

/// <summary>
/// Status transitions are deliberately NOT modeled as mutating methods here —
/// same atomic-repository-level-guard convention as SupportTicket. SourceType/
/// SourceId are additive fields (not in the business specification's own
/// field list) required for ISupportIncidentReporter's idempotency guarantee
/// (a partial unique index on this pair) — null for a manually admin-created
/// incident with no originating event. Latitude/Longitude are validated once
/// at construction via GeoCoordinate.Create (reusing Rides' existing
/// range-check logic) but stored as raw doubles, same interpretation already
/// used for RoadsideAssistanceRequest.
/// </summary>
public sealed class SupportIncident : AggregateRoot
{
    public string IncidentNumber { get; private set; } = string.Empty;
    public SupportIncidentType Type { get; private set; }
    public SupportIncidentSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ReportedByUserId { get; private set; }
    public SupportRelatedEntityType? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public SupportIncidentStatus Status { get; private set; }
    public Guid? AssignedAdminUserId { get; private set; }
    public string? Resolution { get; private set; }

    /// <summary>Idempotency anchor for ISupportIncidentReporter — null for a manually created incident with no originating event.</summary>
    public string? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    private SupportIncident()
    {
    }

    private SupportIncident(
        SupportIncidentType type, SupportIncidentSeverity severity, string title, string description, Guid? reportedByUserId,
        SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId, double? latitude, double? longitude, DateTime occurredAtUtc,
        string? sourceType, Guid? sourceId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        IncidentNumber = GenerateIncidentNumber(utcNow);
        Type = type;
        Severity = severity;
        Title = title;
        Description = description;
        ReportedByUserId = reportedByUserId;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        Latitude = latitude;
        Longitude = longitude;
        OccurredAtUtc = occurredAtUtc;
        SourceType = sourceType;
        SourceId = sourceId;
        Status = SupportIncidentStatus.Reported;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new SupportIncidentReported(Id, type, utcNow));
    }

    public static SupportIncident Create(
        SupportIncidentType type, SupportIncidentSeverity severity, string title, string description, Guid? reportedByUserId,
        SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId, double? latitude, double? longitude, DateTime occurredAtUtc,
        string? sourceType, Guid? sourceId, DateTime utcNow)
    {
        ValidateFields(title, description, relatedEntityType, relatedEntityId, sourceType, sourceId);

        if (latitude is not null || longitude is not null)
        {
            if (latitude is null || longitude is null)
            {
                throw new ArgumentException("La latitude et la longitude doivent être fournies ensemble.");
            }

            GeoCoordinate.Create(latitude.Value, longitude.Value);
        }

        return new SupportIncident(
            type, severity, title.Trim(), description.Trim(), reportedByUserId, relatedEntityType, relatedEntityId, latitude, longitude,
            occurredAtUtc, sourceType, sourceId, utcNow);
    }

    private static void ValidateFields(
        string title, string description, SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId, string? sourceType,
        Guid? sourceId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Un titre est requis.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Une description est requise.");
        }

        if (relatedEntityType is null != relatedEntityId is null)
        {
            throw new ArgumentException("Le type et l'identifiant de l'entité liée doivent être fournis ensemble.");
        }

        if (string.IsNullOrWhiteSpace(sourceType) != (sourceId is null))
        {
            throw new ArgumentException("Le type et l'identifiant de la source doivent être fournis ensemble.");
        }
    }

    private static string GenerateIncidentNumber(DateTime utcNow) =>
        $"INC-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
