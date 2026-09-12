using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents;

/// <summary>Deliberately excludes FileReference — no Application-layer result ever carries the storage key.</summary>
public sealed record VehicleDocumentSummary(
    Guid Id,
    Guid VehicleId,
    VehicleDocumentType DocumentType,
    string FileName,
    string MimeType,
    long FileSize,
    string Sha256,
    VehicleDocumentStatus Status,
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
    public static VehicleDocumentSummary FromEntity(VehicleDocument document, DateTime utcNow, int reminderLeadDays)
    {
        var isNearingExpiration = document.Status == VehicleDocumentStatus.Approved
            && document.ExpirationDate is not null
            && document.ExpirationDate > utcNow
            && document.ExpirationDate <= utcNow.AddDays(reminderLeadDays);

        return new(
            document.Id, document.VehicleId, document.DocumentType, document.FileName, document.MimeType,
            document.FileSize, document.Sha256, document.Status, document.IssueDate, document.ExpirationDate,
            document.Version, document.ReplacesDocumentId, document.ReviewedBy, document.ReviewedAt,
            document.RejectionReason, document.ReviewComment, document.CreatedAt, document.UpdatedAt, isNearingExpiration);
    }
}
