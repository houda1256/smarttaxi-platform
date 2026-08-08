using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetMyFleets;

public sealed class GetMyFleetsQueryHandler : IQueryHandler<GetMyFleetsQuery, IReadOnlyCollection<FleetSummary>>
{
    private readonly IFleetRepository _repository;

    public GetMyFleetsQueryHandler(IFleetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<FleetSummary>> Handle(GetMyFleetsQuery query, CancellationToken cancellationToken)
    {
        var fleets = await _repository.GetForOwnerAsync(query.OwnerId, cancellationToken);

        return fleets.Select(FleetSummary.FromEntity).ToList();
    }
}
