using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContent;

/// <summary>Owner-only self-service download — routine reads of one's own vehicle documents are not audited (proportionality).</summary>
public sealed class GetVehicleDocumentContentQueryHandler : IQueryHandler<GetVehicleDocumentContentQuery, Result<DocumentContentResult>>
{
    private const string NotFoundError = "Document introuvable.";

    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleDocumentAccessAuditRepository _auditRepository;
    private readonly IFileStorageService _fileStorage;

    public GetVehicleDocumentContentQueryHandler(
        IVehicleDocumentRepository documentRepository, IVehicleRepository vehicleRepository,
        IVehicleDocumentAccessAuditRepository auditRepository, IFileStorageService fileStorage)
    {
        _documentRepository = documentRepository;
        _vehicleRepository = vehicleRepository;
        _auditRepository = auditRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<DocumentContentResult>> Handle(GetVehicleDocumentContentQuery query, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentContentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(document.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != query.RequestingUserId)
        {
            return Result<DocumentContentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        // Every content download is audited, own document or not — binary
        // content access is inherently more sensitive than a metadata read.
        await _auditRepository.AddAsync(
            new VehicleDocumentAccessAuditEntry(
                document.Id, query.RequestingUserId, VehicleDocumentAccessType.ContentDownload, DateTime.UtcNow, query.IpAddress),
            cancellationToken);

        var stream = await _fileStorage.OpenReadAsync(document.FileReference, cancellationToken);

        return Result<DocumentContentResult>.Success(new DocumentContentResult(stream, document.MimeType, document.FileName));
    }
}
