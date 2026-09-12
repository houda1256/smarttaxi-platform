using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetAllMaintenanceRequests;

public sealed class GetAllMaintenanceRequestsQueryHandler : IQueryHandler<GetAllMaintenanceRequestsQuery, PagedResult<MaintenanceRequest>>
{
    private readonly IMaintenanceRequestRepository _repository;

    public GetAllMaintenanceRequestsQueryHandler(IMaintenanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<MaintenanceRequest>> Handle(GetAllMaintenanceRequestsQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
