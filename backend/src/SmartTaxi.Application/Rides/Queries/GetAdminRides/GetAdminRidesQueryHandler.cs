using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetAdminRides;

public sealed class GetAdminRidesQueryHandler : IQueryHandler<GetAdminRidesQuery, IReadOnlyCollection<RideSummary>>
{
    private readonly IRideRepository _repository;

    public GetAdminRidesQueryHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<RideSummary>> Handle(GetAdminRidesQuery query, CancellationToken cancellationToken)
    {
        var rides = await _repository.GetAllAsync(query.Status, cancellationToken);

        return rides.Select(RideSummary.FromEntity).ToList();
    }
}
