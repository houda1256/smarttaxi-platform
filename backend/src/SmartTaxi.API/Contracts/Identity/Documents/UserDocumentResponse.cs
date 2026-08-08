using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.API.Contracts.Identity.Documents;

/// <summary>Deliberately has no FileReference field — the storage key never leaves the server.</summary>
public sealed record UserDocumentResponse(
    Guid Id,
    Guid UserId,
    string DocumentType,
    string FileName,
    string MimeType,
    long FileSize,
    string Sha256,
    string Status,
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
    public static UserDocumentResponse FromSummary(UserDocumentSummary summary) => new(
        summary.Id,
        summary.UserId,
        summary.DocumentType.ToString(),
        summary.FileName,
        summary.MimeType,
        summary.FileSize,
        summary.Sha256,
        summary.Status.ToString(),
        summary.IssueDate,
        summary.ExpirationDate,
        summary.Version,
        summary.ReplacesDocumentId,
        summary.ReviewedBy,
        summary.ReviewedAt,
        summary.RejectionReason,
        summary.ReviewComment,
        summary.CreatedAt,
        summary.UpdatedAt,
        summary.IsNearingExpiration);
}
