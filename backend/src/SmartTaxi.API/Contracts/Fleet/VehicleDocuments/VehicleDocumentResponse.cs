using SmartTaxi.Application.Fleet.Vehicles.Documents;

namespace SmartTaxi.API.Contracts.Fleet.VehicleDocuments;

/// <summary>Deliberately has no FileReference field — the storage key never leaves the server.</summary>
public sealed record VehicleDocumentResponse(
    Guid Id,
    Guid VehicleId,
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
    public static VehicleDocumentResponse FromSummary(VehicleDocumentSummary summary) => new(
        summary.Id,
        summary.VehicleId,
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
