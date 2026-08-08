using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional.Abstractions;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetMyProfessionalAccountRequests;

public sealed class GetMyProfessionalAccountRequestsQueryHandler
    : IQueryHandler<GetMyProfessionalAccountRequestsQuery, IReadOnlyCollection<ProfessionalAccountRequestSummary>>
{
    private readonly IProfessionalAccountRequestRepository _repository;

    public GetMyProfessionalAccountRequestsQueryHandler(IProfessionalAccountRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ProfessionalAccountRequestSummary>> Handle(
        GetMyProfessionalAccountRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = await _repository.GetForUserAsync(query.UserId, cancellationToken);

        return requests.Select(ProfessionalAccountRequestSummary.FromEntity).ToList();
    }
}
