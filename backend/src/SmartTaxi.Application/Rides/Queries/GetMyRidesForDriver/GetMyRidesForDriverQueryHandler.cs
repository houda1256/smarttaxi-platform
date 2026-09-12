using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetMyRidesForDriver;

public sealed class GetMyRidesForDriverQueryHandler : IQueryHandler<GetMyRidesForDriverQuery, IReadOnlyCollection<RideSummary>>
{
    private readonly IRideRepository _repository;

    public GetMyRidesForDriverQueryHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<RideSummary>> Handle(GetMyRidesForDriverQuery query, CancellationToken cancellationToken)
    {
        var rides = await _repository.GetForDriverAsync(query.DriverProfileId, cancellationToken);

        return rides.Select(RideSummary.FromEntity).ToList();
    }
}
