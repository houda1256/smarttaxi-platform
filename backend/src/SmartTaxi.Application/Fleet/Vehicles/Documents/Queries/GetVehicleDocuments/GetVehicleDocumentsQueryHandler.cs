using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocuments;

public sealed class GetVehicleDocumentsQueryHandler
    : IQueryHandler<GetVehicleDocumentsQuery, Result<IReadOnlyCollection<VehicleDocumentSummary>>>
{
    private const string NotFoundError = "Véhicule introuvable.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetVehicleDocumentsQueryHandler(
        IVehicleRepository vehicleRepository, IVehicleDocumentRepository documentRepository,
        IDocumentExpirationPolicy expirationPolicy)
    {
        _vehicleRepository = vehicleRepository;
        _documentRepository = documentRepository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<Result<IReadOnlyCollection<VehicleDocumentSummary>>> Handle(
        GetVehicleDocumentsQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != query.RequestingUserId)
        {
            return Result<IReadOnlyCollection<VehicleDocumentSummary>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        await _documentRepository.ExpireDueDocumentsAsync(utcNow, cancellationToken);

        var documents = await _documentRepository.GetForVehicleAsync(query.VehicleId, cancellationToken);

        return Result<IReadOnlyCollection<VehicleDocumentSummary>>.Success(
            documents.Select(d => VehicleDocumentSummary.FromEntity(d, utcNow, _expirationPolicy.ReminderLeadDays)).ToList());
    }
}
