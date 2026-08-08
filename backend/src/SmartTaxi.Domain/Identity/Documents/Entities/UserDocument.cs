using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Domain.Identity.Documents.Entities;

/// <summary>
/// Status transitions (Approve/Reject/Suspend/Cancel/Expire/Replace) are
/// deliberately not modeled as mutating methods here — they are enforced as
/// atomic, race-safe conditional SQL updates in the repository (the same
/// pattern established in sub-slice 2c for single-use tokens), so the guard
/// condition has exactly one source of truth instead of being duplicated
/// between an in-memory check and a SQL WHERE clause.
/// </summary>
public sealed class UserDocument
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string FileReference { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
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

    private UserDocument()
    {
    }

    private UserDocument(
        Guid userId,
        DocumentType documentType,
        string fileReference,
        string fileName,
        string mimeType,
        long fileSize,
        string sha256,
        DateTime? issueDate,
        DateTime? expirationDate,
        int version,
        Guid? replacesDocumentId,
        DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DocumentType = documentType;
        FileReference = fileReference;
        FileName = fileName;
        MimeType = mimeType;
        FileSize = fileSize;
        Sha256 = sha256;
        Status = DocumentStatus.Pending;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Version = version;
        ReplacesDocumentId = replacesDocumentId;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static UserDocument Upload(
        Guid userId,
        DocumentType documentType,
        string fileReference,
        string fileName,
        string mimeType,
        long fileSize,
        string sha256,
        DateTime? issueDate,
        DateTime? expirationDate,
        DateTime utcNow) =>
        new(userId, documentType, fileReference, fileName, mimeType, fileSize, sha256, issueDate, expirationDate,
            version: 1, replacesDocumentId: null, utcNow);

    /// <summary>
    /// Builds the next version in this document's chain. Does not mutate this
    /// instance — the caller (repository) is responsible for atomically
    /// transitioning this row to Replaced in the same operation.
    /// </summary>
    public UserDocument CreateReplacement(
        string fileReference,
        string fileName,
        string mimeType,
        long fileSize,
        string sha256,
        DateTime? issueDate,
        DateTime? expirationDate,
        DateTime utcNow) =>
        new(UserId, DocumentType, fileReference, fileName, mimeType, fileSize, sha256, issueDate, expirationDate,
            Version + 1, Id, utcNow);

    /// <summary>
    /// A document is only currently valid/eligible while Approved and not past
    /// its expiration date — every other status (including Cancelled) is not
    /// reviewable or eligible.
    /// </summary>
    public bool IsCurrentlyValid(DateTime utcNow) =>
        Status == DocumentStatus.Approved && (ExpirationDate is null || ExpirationDate > utcNow);
}
