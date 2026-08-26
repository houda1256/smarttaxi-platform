using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMaintenanceRequestDetails;

public sealed record GetMaintenanceRequestDetailsQuery(Guid RequestId, Guid RequestingUserId) : IQuery<Result<MaintenanceRequest>>;
