using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents;

/// <summary>
/// Deliberately excludes FileReference (the opaque storage key) — no
/// Application-layer result ever carries it, so the API layer cannot
/// accidentally leak it even if a contract mapping were careless.
/// </summary>
public sealed record UserDocumentSummary(
    Guid Id,
    Guid UserId,
    DocumentType DocumentType,
    string FileName,
    string MimeType,
    long FileSize,
    string Sha256,
    DocumentStatus Status,
    DateTime? IssueDate,
    DateTime? ExpirationDate,
    int Version,
    Guid? ReplacesDocumentId,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string? RejectionReason,
    string? ReviewComment,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsNearingExpiration)
{
    public static UserDocumentSummary FromEntity(UserDocument document, DateTime utcNow, int reminderLeadDays)
    {
        var isNearingExpiration = document.Status == DocumentStatus.Approved
            && document.ExpirationDate is not null
            && document.ExpirationDate > utcNow
            && document.ExpirationDate <= utcNow.AddDays(reminderLeadDays);

        return new(
            document.Id,
            document.UserId,
            document.DocumentType,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.Sha256,
            document.Status,
            document.IssueDate,
            document.ExpirationDate,
            document.Version,
            document.ReplacesDocumentId,
            document.ReviewedBy,
            document.ReviewedAt,
            document.RejectionReason,
            document.ReviewComment,
            document.CreatedAt,
            document.UpdatedAt,
            isNearingExpiration);
    }
}
