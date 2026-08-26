using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetAllRoadsideAssistanceRequests;

public sealed class GetAllRoadsideAssistanceRequestsQueryHandler : IQueryHandler<GetAllRoadsideAssistanceRequestsQuery, PagedResult<RoadsideAssistanceRequest>>
{
    private readonly IRoadsideAssistanceRequestRepository _repository;

    public GetAllRoadsideAssistanceRequestsQueryHandler(IRoadsideAssistanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<RoadsideAssistanceRequest>> Handle(GetAllRoadsideAssistanceRequestsQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
