using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContentAdmin;

public sealed class GetVehicleDocumentContentAdminQueryHandler
    : IQueryHandler<GetVehicleDocumentContentAdminQuery, Result<DocumentContentResult>>
{
    private const string NotFoundError = "Document introuvable.";

    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IVehicleDocumentAccessAuditRepository _auditRepository;
    private readonly IFileStorageService _fileStorage;

    public GetVehicleDocumentContentAdminQueryHandler(
        IVehicleDocumentRepository documentRepository, IVehicleDocumentAccessAuditRepository auditRepository,
        IFileStorageService fileStorage)
    {
        _documentRepository = documentRepository;
        _auditRepository = auditRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<DocumentContentResult>> Handle(
        GetVehicleDocumentContentAdminQuery query, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentContentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        await _auditRepository.AddAsync(
            new VehicleDocumentAccessAuditEntry(
                document.Id, query.RequestedByUserId, VehicleDocumentAccessType.ContentDownload, DateTime.UtcNow, query.IpAddress),
            cancellationToken);

        var stream = await _fileStorage.OpenReadAsync(document.FileReference, cancellationToken);

        return Result<DocumentContentResult>.Success(new DocumentContentResult(stream, document.MimeType, document.FileName));
    }
}
