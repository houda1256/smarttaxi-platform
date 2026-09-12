using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;

public interface IVehicleDocumentRepository
{
    Task AddAsync(VehicleDocument document, CancellationToken cancellationToken);

    Task<VehicleDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehicleDocument>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehicleDocument>> GetPendingAsync(CancellationToken cancellationToken);

    Task<VehicleDocument?> GetLatestForVehicleAndTypeAsync(
        Guid vehicleId, VehicleDocumentType documentType, CancellationToken cancellationToken);

    Task<bool> ExistsWithHashAsync(Guid vehicleId, VehicleDocumentType documentType, string sha256, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(VehicleDocument current, VehicleDocument replacement, DateTime utcNow, CancellationToken cancellationToken);
}
