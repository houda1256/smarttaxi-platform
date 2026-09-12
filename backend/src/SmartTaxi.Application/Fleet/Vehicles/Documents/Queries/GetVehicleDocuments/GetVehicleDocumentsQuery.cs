using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Documents;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocuments;

public sealed record GetVehicleDocumentsQuery(Guid RequestingUserId, Guid VehicleId)
    : IQuery<Result<IReadOnlyCollection<VehicleDocumentSummary>>>;
