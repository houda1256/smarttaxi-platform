using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetPendingPersonalDataRequestsAdmin;

public sealed class GetPendingPersonalDataRequestsAdminQueryHandler
    : IQueryHandler<GetPendingPersonalDataRequestsAdminQuery, IReadOnlyCollection<PersonalDataRequestSummary>>
{
    private readonly IPersonalDataRequestRepository _repository;

    public GetPendingPersonalDataRequestsAdminQueryHandler(IPersonalDataRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PersonalDataRequestSummary>> Handle(
        GetPendingPersonalDataRequestsAdminQuery query, CancellationToken cancellationToken)
    {
        var requests = await _repository.GetPendingAsync(cancellationToken);

        return requests.Select(PersonalDataRequestSummary.FromEntity).ToList();
    }
}
