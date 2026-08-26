using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Domain.Support.Events;

namespace SmartTaxi.Domain.Support.Entities;

/// <summary>
/// Status transitions are deliberately NOT modeled as mutating methods here —
/// they are enforced as atomic, race-safe conditional SQL updates in the
/// repository (same convention as MaintenanceRequest/RoadsideAssistanceRequest),
/// so the guard condition has exactly one source of truth. This entity only
/// owns field-level construction validation. RequesterUserId/AssignedAdminUserId
/// are plain Guid references (no FK), same convention every module uses for
/// referencing Identity. RelatedEntityType/RelatedEntityId are plain
/// references too — referential integrity against the real owning module is
/// enforced entirely at the Application layer, never a DB FK (no safe
/// polymorphic FK exists in PostgreSQL).
/// </summary>
public sealed class SupportTicket : AggregateRoot
{
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid RequesterUserId { get; private set; }
    public SupportTicketCategory Category { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public SupportTicketPriority Priority { get; private set; }
    public SupportRelatedEntityType? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    public SupportTicketStatus Status { get; private set; }
    public Guid? AssignedAdminUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime? ReopenedAtUtc { get; private set; }
    public string? Resolution { get; private set; }

    /// <summary>Set exactly once — also the idempotency guard preventing a retry from creating a second SupportIncident.</summary>
    public Guid? EscalatedIncidentId { get; private set; }

    private SupportTicket()
    {
    }

    private SupportTicket(
        Guid requesterUserId, SupportTicketCategory category, string subject, string description,
        SupportTicketPriority priority, SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        TicketNumber = GenerateTicketNumber(utcNow);
        RequesterUserId = requesterUserId;
        Category = category;
        Subject = subject;
        Description = description;
        Priority = priority;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        Status = SupportTicketStatus.Open;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new SupportTicketCreated(Id, requesterUserId, utcNow));
    }

    public static SupportTicket Create(
        Guid requesterUserId, SupportTicketCategory category, string subject, string description,
        SupportTicketPriority priority, SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId, DateTime utcNow)
    {
        ValidateFields(requesterUserId, subject, description, relatedEntityType, relatedEntityId);

        return new SupportTicket(
            requesterUserId, category, subject.Trim(), description.Trim(), priority, relatedEntityType, relatedEntityId, utcNow);
    }

    private static void ValidateFields(
        Guid requesterUserId, string subject, string description, SupportRelatedEntityType? relatedEntityType, Guid? relatedEntityId)
    {
        if (requesterUserId == Guid.Empty)
        {
            throw new ArgumentException("Le demandeur est requis.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Un sujet est requis.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Une description est requise.");
        }

        if (relatedEntityType is null != relatedEntityId is null)
        {
            throw new ArgumentException("Le type et l'identifiant de l'entité liée doivent être fournis ensemble.");
        }
    }

    private static string GenerateTicketNumber(DateTime utcNow) =>
        $"SUP-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
