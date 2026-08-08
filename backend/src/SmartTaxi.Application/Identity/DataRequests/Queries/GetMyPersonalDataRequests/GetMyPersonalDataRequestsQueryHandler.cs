using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataRequests;

public sealed class GetMyPersonalDataRequestsQueryHandler
    : IQueryHandler<GetMyPersonalDataRequestsQuery, IReadOnlyCollection<PersonalDataRequestSummary>>
{
    private readonly IPersonalDataRequestRepository _repository;

    public GetMyPersonalDataRequestsQueryHandler(IPersonalDataRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PersonalDataRequestSummary>> Handle(
        GetMyPersonalDataRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = await _repository.GetForUserAsync(query.UserId, cancellationToken);

        return requests.Select(PersonalDataRequestSummary.FromEntity).ToList();
    }
}
