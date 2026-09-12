using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

/// <summary>
/// Mirrors SmartTaxi.Domain.Identity.Documents.Entities.UserDocument's proven
/// shape (owned by VehicleId instead of UserId) — a deliberately separate
/// aggregate per instructions, not forced into UserDocument. Status
/// transitions are enforced as atomic repository-level guards, not domain
/// methods, same as UserDocument and every prior sub-slice.
/// </summary>
public sealed class VehicleDocument
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public VehicleDocumentType DocumentType { get; private set; }
    public string FileReference { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public VehicleDocumentStatus Status { get; private set; }
    public DateTime? IssueDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public int Version { get; private set; }
    public Guid? ReplacesDocumentId { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ReviewComment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private VehicleDocument()
    {
    }

    private VehicleDocument(
        Guid vehicleId, VehicleDocumentType documentType, string fileReference, string fileName, string mimeType,
        long fileSize, string sha256, DateTime? issueDate, DateTime? expirationDate, int version,
        Guid? replacesDocumentId, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        DocumentType = documentType;
        FileReference = fileReference;
        FileName = fileName;
        MimeType = mimeType;
        FileSize = fileSize;
        Sha256 = sha256;
        Status = VehicleDocumentStatus.Pending;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Version = version;
        ReplacesDocumentId = replacesDocumentId;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static VehicleDocument Upload(
        Guid vehicleId, VehicleDocumentType documentType, string fileReference, string fileName, string mimeType,
        long fileSize, string sha256, DateTime? issueDate, DateTime? expirationDate, DateTime utcNow) =>
        new(vehicleId, documentType, fileReference, fileName, mimeType, fileSize, sha256, issueDate, expirationDate,
            version: 1, replacesDocumentId: null, utcNow);

    public VehicleDocument CreateReplacement(
        string fileReference, string fileName, string mimeType, long fileSize, string sha256,
        DateTime? issueDate, DateTime? expirationDate, DateTime utcNow) =>
        new(VehicleId, DocumentType, fileReference, fileName, mimeType, fileSize, sha256, issueDate, expirationDate,
            Version + 1, Id, utcNow);

    public bool IsCurrentlyValid(DateTime utcNow) =>
        Status == VehicleDocumentStatus.Approved && (ExpirationDate is null || ExpirationDate > utcNow);
}
