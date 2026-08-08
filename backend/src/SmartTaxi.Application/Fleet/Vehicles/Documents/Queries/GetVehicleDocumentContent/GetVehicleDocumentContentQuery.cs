using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContent;

public sealed record GetVehicleDocumentContentQuery(Guid RequestingUserId, Guid DocumentId, string? IpAddress)
    : IQuery<Result<DocumentContentResult>>;
