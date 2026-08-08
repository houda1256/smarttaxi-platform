using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SearchDrivers;

/// <summary>
/// Draft→Searching→{DriversAvailable|NoDriverAvailable}. The ranked result is
/// persisted as a snapshot (RideDriverRecommendation rows) so the list a
/// Customer later reads via GetRecommendedDrivers is stable, not recomputed
/// on every read.
/// </summary>
public sealed class SearchDriversCommandHandler : ICommandHandler<SearchDriversCommand, Result<int>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotDraftError = "Cette course n'est pas en attente de recherche.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideDriverRecommendationRepository _recommendationRepository;
    private readonly IDriverRecommendationService _recommendationService;

    public SearchDriversCommandHandler(
        IRideRepository rideRepository, IRideDriverRecommendationRepository recommendationRepository,
        IDriverRecommendationService recommendationService)
    {
        _rideRepository = rideRepository;
        _recommendationRepository = recommendationRepository;
        _recommendationService = recommendationService;
    }

    public async Task<Result<int>> Handle(SearchDriversCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<int>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.Status != RideStatus.Draft)
        {
            return Result<int>.Failure(NotDraftError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;

        var enteredSearching = await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.Draft, RideStatus.Searching, command.RequestingUserId, null, utcNow, cancellationToken);

        if (!enteredSearching)
        {
            return Result<int>.Failure(NotDraftError, ErrorType.Conflict);
        }

        var criteria = new DriverRecommendationCriteria(
            ride.PickupLocation, ride.PreferredVehicleCategory, ride.PassengerCount, ride.NeedsAccessibleVehicle,
            ride.NeedsAirConditioning);

        var recommendations = await _recommendationService.GetRecommendationsAsync(criteria, cancellationToken);

        if (recommendations.Count == 0)
        {
            await _rideRepository.TryTransitionAsync(
                ride.Id, RideStatus.Searching, RideStatus.NoDriverAvailable, null, "Aucun chauffeur disponible",
                DateTime.UtcNow, cancellationToken);

            return Result<int>.Success(0);
        }

        var snapshots = recommendations
            .Select((recommendation, index) => new RideDriverRecommendation(
                ride.Id, recommendation.DriverId, recommendation.VehicleId, recommendation.DistanceToPickupKm,
                recommendation.EstimatedArrivalMinutes, recommendation.RecommendationScore,
                string.Join("; ", recommendation.RecommendationReasons), index + 1, DateTime.UtcNow))
            .ToList();

        await _recommendationRepository.ReplaceForRideAsync(ride.Id, snapshots, cancellationToken);

        await _rideRepository.TryTransitionAsync(
            ride.Id, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, cancellationToken);

        return Result<int>.Success(recommendations.Count);
    }
}
