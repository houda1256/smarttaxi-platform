using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideAssistanceRequests;

public sealed class GetMyRoadsideAssistanceRequestsQueryHandler : IQueryHandler<GetMyRoadsideAssistanceRequestsQuery, PagedResult<RoadsideAssistanceRequest>>
{
    private readonly IRoadsideAssistanceRequestRepository _repository;

    public GetMyRoadsideAssistanceRequestsQueryHandler(IRoadsideAssistanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<RoadsideAssistanceRequest>> Handle(GetMyRoadsideAssistanceRequestsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForRequesterAsync(query.RequesterUserId, query.PageNumber, query.PageSize, cancellationToken);
}
