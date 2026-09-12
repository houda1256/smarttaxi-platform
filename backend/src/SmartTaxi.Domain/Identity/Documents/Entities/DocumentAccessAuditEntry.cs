using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Domain.Identity.Documents.Entities;

public sealed class DocumentAccessAuditEntry
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid AccessedByUserId { get; private set; }
    public DocumentAccessType AccessType { get; private set; }
    public DateTime AccessedAt { get; private set; }
    public string? IpAddress { get; private set; }

    private DocumentAccessAuditEntry()
    {
    }

    public DocumentAccessAuditEntry(
        Guid documentId, Guid accessedByUserId, DocumentAccessType accessType, DateTime utcNow, string? ipAddress)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        AccessedByUserId = accessedByUserId;
        AccessType = accessType;
        AccessedAt = utcNow;
        IpAddress = ipAddress;
    }
}
