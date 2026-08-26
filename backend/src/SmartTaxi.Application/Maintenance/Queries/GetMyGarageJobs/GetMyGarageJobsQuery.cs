using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyGarageJobs;

public sealed record GetMyGarageJobsQuery(Guid GarageUserId, int PageNumber, int PageSize) : IQuery<PagedResult<MaintenanceRequest>>;
