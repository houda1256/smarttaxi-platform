using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetPendingVehicleDocuments;

public sealed class GetPendingVehicleDocumentsQueryHandler
    : IQueryHandler<GetPendingVehicleDocumentsQuery, IReadOnlyCollection<VehicleDocumentSummary>>
{
    private readonly IVehicleDocumentRepository _repository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetPendingVehicleDocumentsQueryHandler(IVehicleDocumentRepository repository, IDocumentExpirationPolicy expirationPolicy)
    {
        _repository = repository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<IReadOnlyCollection<VehicleDocumentSummary>> Handle(
        GetPendingVehicleDocumentsQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var documents = await _repository.GetPendingAsync(cancellationToken);

        return documents.Select(d => VehicleDocumentSummary.FromEntity(d, utcNow, _expirationPolicy.ReminderLeadDays)).ToList();
    }
}
