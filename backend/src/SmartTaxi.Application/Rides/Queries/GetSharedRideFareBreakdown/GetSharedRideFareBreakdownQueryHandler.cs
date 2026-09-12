using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Rides.Queries.GetSharedRideFareBreakdown;

public sealed class GetSharedRideFareBreakdownQueryHandler
    : IQueryHandler<GetSharedRideFareBreakdownQuery, Result<IReadOnlyCollection<SharedRideFareShare>>>
{
    private const string NotFoundError = "Proposition de trajet partagé introuvable.";

    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly ISharedRideParticipantRepository _participantRepository;
    private readonly IRideRepository _rideRepository;
    private readonly IRouteEstimationService _routeEstimationService;
    private readonly IFareCalculator _fareCalculator;

    public GetSharedRideFareBreakdownQueryHandler(
        ISharedRideMatchRepository matchRepository, ISharedRideParticipantRepository participantRepository,
        IRideRepository rideRepository, IRouteEstimationService routeEstimationService, IFareCalculator fareCalculator)
    {
        _matchRepository = matchRepository;
        _participantRepository = participantRepository;
        _rideRepository = rideRepository;
        _routeEstimationService = routeEstimationService;
        _fareCalculator = fareCalculator;
    }

    public async Task<Result<IReadOnlyCollection<SharedRideFareShare>>> Handle(
        GetSharedRideFareBreakdownQuery query, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(query.MatchId, cancellationToken);

        if (match is null)
        {
            return Result<IReadOnlyCollection<SharedRideFareShare>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var participants = await _participantRepository.GetForMatchAsync(match.Id, cancellationToken);
        var rideRoutes = new List<(Guid RideId, Guid CustomerId, decimal DistanceKm, int DurationMinutes)>();

        foreach (var participant in participants)
        {
            var ride = await _rideRepository.GetByIdAsync(participant.RideId, cancellationToken);

            if (ride is null)
            {
                continue;
            }

            var route = _routeEstimationService.Estimate(ride.PickupLocation, ride.DestinationLocation);
            rideRoutes.Add((ride.Id, ride.CustomerId, route.DistanceKm, route.DurationMinutes));
        }

        if (rideRoutes.Count != 2)
        {
            return Result<IReadOnlyCollection<SharedRideFareShare>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var commonDistanceKm = Math.Min(rideRoutes[0].DistanceKm, rideRoutes[1].DistanceKm);
        var commonDurationMinutes = Math.Min(rideRoutes[0].DurationMinutes, rideRoutes[1].DurationMinutes);

        var commonFare = _fareCalculator.Calculate(new FareCalculationInput(commonDistanceKm, commonDurationMinutes, VehicleCategory.Standard, DynamicMultiplier: 1m));
        var commonRouteShare = Math.Round((commonFare.BaseFare + commonFare.DistanceFare + commonFare.DurationFare) / 2, 2);
        var bookingFeeShare = Math.Round(commonFare.BookingFee / 2, 2);

        var shares = new List<SharedRideFareShare>();

        foreach (var (rideId, customerId, distanceKm, durationMinutes) in rideRoutes)
        {
            var detourDistanceKm = Math.Max(distanceKm - commonDistanceKm, 0);
            var detourDurationMinutes = Math.Max(durationMinutes - commonDurationMinutes, 0);
            var detourFare = _fareCalculator.Calculate(new FareCalculationInput(detourDistanceKm, detourDurationMinutes, VehicleCategory.Standard, DynamicMultiplier: 1m));
            var personalDetourFare = Math.Round(detourFare.DistanceFare + detourFare.DurationFare, 2);
            var total = commonRouteShare + bookingFeeShare + personalDetourFare;

            shares.Add(new SharedRideFareShare(rideId, customerId, commonRouteShare, personalDetourFare, bookingFeeShare, total));
        }

        return Result<IReadOnlyCollection<SharedRideFareShare>>.Success(shares);
    }
}
