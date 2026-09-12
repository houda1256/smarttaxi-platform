using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SelectDriver;

/// <summary>
/// The Customer always makes the final selection — this command never picks
/// a Driver on the Customer's behalf, it only validates and reserves the one
/// they chose from their own previously-returned recommendation list. The
/// Driver-side unique-active-hold DB constraint (see
/// IDriverReservationHoldRepository) is what makes two concurrent Customers
/// unable to both reserve the same Driver.
/// </summary>
public sealed class SelectDriverCommandHandler : ICommandHandler<SelectDriverCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotDriversAvailableError = "Cette course n'est pas en attente de sélection.";
    private const string NotRecommendedError = "Ce chauffeur ne fait pas partie des chauffeurs recommandés pour cette course.";
    private const string DriverAlreadyReservedError = "Ce chauffeur est déjà réservé pour une autre demande.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideDriverRecommendationRepository _recommendationRepository;
    private readonly IDriverReservationHoldRepository _holdRepository;
    private readonly IDriverSearchPolicy _searchPolicy;

    public SelectDriverCommandHandler(
        IRideRepository rideRepository, IRideDriverRecommendationRepository recommendationRepository,
        IDriverReservationHoldRepository holdRepository, IDriverSearchPolicy searchPolicy)
    {
        _rideRepository = rideRepository;
        _recommendationRepository = recommendationRepository;
        _holdRepository = holdRepository;
        _searchPolicy = searchPolicy;
    }

    public async Task<Result<Guid>> Handle(SelectDriverCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.Status != RideStatus.DriversAvailable)
        {
            return Result<Guid>.Failure(NotDriversAvailableError, ErrorType.Conflict);
        }

        var recommendations = await _recommendationRepository.GetForRideAsync(ride.Id, cancellationToken);

        if (!recommendations.Any(r => r.DriverId == command.DriverId && r.VehicleId == command.VehicleId))
        {
            return Result<Guid>.Failure(NotRecommendedError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var hold = DriverReservationHold.CreateActive(
            ride.Id, command.DriverId, command.VehicleId, utcNow, TimeSpan.FromSeconds(_searchPolicy.DriverResponseTimeoutSeconds));

        var holdCreated = await _holdRepository.TryAddAsync(hold, cancellationToken);

        if (!holdCreated)
        {
            return Result<Guid>.Failure(DriverAlreadyReservedError, ErrorType.Conflict);
        }

        var rideUpdated = await _rideRepository.TrySelectDriverAsync(ride.Id, command.DriverId, command.VehicleId, utcNow, cancellationToken);

        if (!rideUpdated)
        {
            // The Ride moved on concurrently (e.g. Customer cancelled) — undo the hold we just took.
            await _holdRepository.TryReleaseAsync(hold.Id, utcNow, cancellationToken);
            return Result<Guid>.Failure(NotDriversAvailableError, ErrorType.Conflict);
        }

        return Result<Guid>.Success(hold.Id);
    }
}
