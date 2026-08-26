using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideJobs;

public sealed class GetMyRoadsideJobsQueryHandler : IQueryHandler<GetMyRoadsideJobsQuery, PagedResult<RoadsideAssistanceRequest>>
{
    private readonly IRoadsideAssistanceRequestRepository _repository;

    public GetMyRoadsideJobsQueryHandler(IRoadsideAssistanceRequestRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<RoadsideAssistanceRequest>> Handle(GetMyRoadsideJobsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForPartnerAsync(query.PartnerUserId, query.PageNumber, query.PageSize, cancellationToken);
}
