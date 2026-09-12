using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

public sealed class VehicleDocumentAccessAuditEntry
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid AccessedByUserId { get; private set; }
    public VehicleDocumentAccessType AccessType { get; private set; }
    public DateTime AccessedAt { get; private set; }
    public string? IpAddress { get; private set; }

    private VehicleDocumentAccessAuditEntry()
    {
    }

    public VehicleDocumentAccessAuditEntry(
        Guid documentId, Guid accessedByUserId, VehicleDocumentAccessType accessType, DateTime utcNow, string? ipAddress)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        AccessedByUserId = accessedByUserId;
        AccessType = accessType;
        AccessedAt = utcNow;
        IpAddress = ipAddress;
    }
}
