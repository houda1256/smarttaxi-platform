using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Queries.GetPendingRideRequestsForDriver;

/// <summary>Bounded to at most one item in practice — the DB-level unique active-hold constraint means a Driver can never have two simultaneous pending requests.</summary>
public sealed class GetPendingRideRequestsForDriverQueryHandler
    : IQueryHandler<GetPendingRideRequestsForDriverQuery, IReadOnlyCollection<RideSummary>>
{
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideRepository _rideRepository;

    public GetPendingRideRequestsForDriverQueryHandler(IDriverProfileRepository driverRepository, IRideRepository rideRepository)
    {
        _driverRepository = driverRepository;
        _rideRepository = rideRepository;
    }

    public async Task<IReadOnlyCollection<RideSummary>> Handle(
        GetPendingRideRequestsForDriverQuery query, CancellationToken cancellationToken)
    {
        var driver = await _driverRepository.GetByUserIdAsync(query.DriverUserId, cancellationToken);

        if (driver is null)
        {
            return [];
        }

        var rides = await _rideRepository.GetForDriverAsync(driver.Id, cancellationToken);

        return rides.Where(r => r.Status == RideStatus.PendingDriverResponse).Select(RideSummary.FromEntity).ToList();
    }
}
