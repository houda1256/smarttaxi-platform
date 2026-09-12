using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyMaintenanceRequests;

public sealed class GetMyMaintenanceRequestsQueryHandler : IQueryHandler<GetMyMaintenanceRequestsQuery, PagedResult<MaintenanceRequest>>
{
    private readonly IMaintenanceRequestRepository _repository;

    public GetMyMaintenanceRequestsQueryHandler(IMaintenanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<MaintenanceRequest>> Handle(GetMyMaintenanceRequestsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForOwnerAsync(query.OwnerUserId, query.PageNumber, query.PageSize, cancellationToken);
}
