using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyMaintenanceRequests;

public sealed record GetMyMaintenanceRequestsQuery(Guid OwnerUserId, int PageNumber, int PageSize) : IQuery<PagedResult<MaintenanceRequest>>;
