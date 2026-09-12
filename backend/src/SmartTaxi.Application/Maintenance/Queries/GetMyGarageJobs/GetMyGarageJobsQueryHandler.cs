using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyGarageJobs;

public sealed class GetMyGarageJobsQueryHandler : IQueryHandler<GetMyGarageJobsQuery, PagedResult<MaintenanceRequest>>
{
    private readonly IMaintenanceRequestRepository _repository;

    public GetMyGarageJobsQueryHandler(IMaintenanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<MaintenanceRequest>> Handle(GetMyGarageJobsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForGarageAsync(query.GarageUserId, query.PageNumber, query.PageSize, cancellationToken);
}
