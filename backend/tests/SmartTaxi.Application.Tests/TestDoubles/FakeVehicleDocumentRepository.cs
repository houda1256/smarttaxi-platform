using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
{
    private readonly Dictionary<Guid, VehicleDocument> _documentsById = new();

    public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
    {
        _documentsById[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<VehicleDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(_documentsById.GetValueOrDefault(documentId));

    public Task<IReadOnlyCollection<VehicleDocument>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<VehicleDocument> documents = _documentsById.Values.Where(d => d.VehicleId == vehicleId).ToList();
        return Task.FromResult(documents);
    }

    public Task<IReadOnlyCollection<VehicleDocument>> GetPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<VehicleDocument> documents =
            _documentsById.Values.Where(d => d.Status == VehicleDocumentStatus.Pending).ToList();
        return Task.FromResult(documents);
    }

    public Task<VehicleDocument?> GetLatestForVehicleAndTypeAsync(
        Guid vehicleId, VehicleDocumentType documentType, CancellationToken cancellationToken)
    {
        var latest = _documentsById.Values
            .Where(d => d.VehicleId == vehicleId && d.DocumentType == documentType && d.Status != VehicleDocumentStatus.Replaced)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<bool> ExistsWithHashAsync(
        Guid vehicleId, VehicleDocumentType documentType, string sha256, CancellationToken cancellationToken)
    {
        var exists = _documentsById.Values.Any(d =>
            d.VehicleId == vehicleId && d.DocumentType == documentType && d.Sha256 == sha256
            && d.Status != VehicleDocumentStatus.Replaced);

        return Task.FromResult(exists);
    }

    public Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != VehicleDocumentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(VehicleDocument.Status), VehicleDocumentStatus.Approved);
        SetProperty(document, nameof(VehicleDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(VehicleDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(VehicleDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(VehicleDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != VehicleDocumentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(VehicleDocument.Status), VehicleDocumentStatus.Rejected);
        SetProperty(document, nameof(VehicleDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(VehicleDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(VehicleDocument.RejectionReason), rejectionReason);
        SetProperty(document, nameof(VehicleDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(VehicleDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != VehicleDocumentStatus.Approved)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(VehicleDocument.Status), VehicleDocumentStatus.Suspended);
        SetProperty(document, nameof(VehicleDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(VehicleDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(VehicleDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(VehicleDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var due = _documentsById.Values
            .Where(d => d.Status == VehicleDocumentStatus.Approved && d.ExpirationDate is not null && d.ExpirationDate <= utcNow)
            .ToList();

        foreach (var document in due)
        {
            SetProperty(document, nameof(VehicleDocument.Status), VehicleDocumentStatus.Expired);
            SetProperty(document, nameof(VehicleDocument.UpdatedAt), utcNow);
        }

        return Task.FromResult(due.Count);
    }

    public Task<bool> TryReplaceAsync(
        VehicleDocument current, VehicleDocument replacement, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(current.Id, out var existing) || existing.Status == VehicleDocumentStatus.Replaced)
        {
            return Task.FromResult(false);
        }

        SetProperty(existing, nameof(VehicleDocument.Status), VehicleDocumentStatus.Replaced);
        SetProperty(existing, nameof(VehicleDocument.UpdatedAt), utcNow);
        _documentsById[replacement.Id] = replacement;
        return Task.FromResult(true);
    }

    public int Count => _documentsById.Count;

    private static void SetProperty(VehicleDocument document, string propertyName, object? value) =>
        typeof(VehicleDocument).GetProperty(propertyName)!.SetValue(document, value);
}
