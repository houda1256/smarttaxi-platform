using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetActiveRidesAdmin;

public sealed class GetActiveRidesAdminQueryHandler : IQueryHandler<GetActiveRidesAdminQuery, IReadOnlyCollection<RideSummary>>
{
    private readonly IRideRepository _repository;

    public GetActiveRidesAdminQueryHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<RideSummary>> Handle(GetActiveRidesAdminQuery query, CancellationToken cancellationToken)
    {
        var rides = await _repository.GetActiveAsync(cancellationToken);

        return rides.Select(RideSummary.FromEntity).ToList();
    }
}
