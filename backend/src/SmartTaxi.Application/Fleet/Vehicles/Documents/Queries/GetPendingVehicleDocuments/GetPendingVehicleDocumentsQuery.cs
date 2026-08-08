using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Documents;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetPendingVehicleDocuments;

public sealed record GetPendingVehicleDocumentsQuery : IQuery<IReadOnlyCollection<VehicleDocumentSummary>>;
