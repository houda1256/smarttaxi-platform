using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional.Abstractions;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetPendingProfessionalAccountRequests;

public sealed class GetPendingProfessionalAccountRequestsQueryHandler
    : IQueryHandler<GetPendingProfessionalAccountRequestsQuery, IReadOnlyCollection<ProfessionalAccountRequestSummary>>
{
    private readonly IProfessionalAccountRequestRepository _repository;

    public GetPendingProfessionalAccountRequestsQueryHandler(IProfessionalAccountRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ProfessionalAccountRequestSummary>> Handle(
        GetPendingProfessionalAccountRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = await _repository.GetPendingAsync(cancellationToken);

        return requests.Select(ProfessionalAccountRequestSummary.FromEntity).ToList();
    }
}
