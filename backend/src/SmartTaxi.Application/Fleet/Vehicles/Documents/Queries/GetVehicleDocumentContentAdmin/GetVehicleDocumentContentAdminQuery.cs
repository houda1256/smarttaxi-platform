using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContentAdmin;

public sealed record GetVehicleDocumentContentAdminQuery(Guid RequestedByUserId, Guid DocumentId, string? IpAddress)
    : IQuery<Result<DocumentContentResult>>;
