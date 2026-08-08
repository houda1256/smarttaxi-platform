using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.ApproveSharedRideByDriver;

/// <summary>
/// Vehicle seat capacity is deliberately checked here, not at the initial
/// trip-compatibility matching stage — no specific vehicle exists yet at
/// that point. The Driver's acceptance both confirms the Match and jumps
/// each paired Ride straight to DriverAccepted (see
/// IRideRepository.TryConfirmSharedRideDriverAsync) — no separate
/// hold/accept dance per Ride, since the Driver's single acceptance here
/// covers both.
/// </summary>
public sealed class ApproveSharedRideByDriverCommandHandler : ICommandHandler<ApproveSharedRideByDriverCommand, Result>
{
    private const string NotFoundError = "Proposition de trajet partagé introuvable.";
    private const string NotWaitingForDriverError = "Cette proposition n'est pas en attente d'approbation du chauffeur.";
    private const string DriverNotFoundError = "Profil chauffeur introuvable.";
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string InsufficientCapacityError = "Le véhicule n'a pas une capacité suffisante pour ce trajet partagé.";
    private const string RideNoLongerAvailableError = "L'une des courses associées n'est plus disponible pour une confirmation.";

    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly ISharedRideParticipantRepository _participantRepository;
    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public ApproveSharedRideByDriverCommandHandler(
        ISharedRideMatchRepository matchRepository, ISharedRideParticipantRepository participantRepository,
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IVehicleRepository vehicleRepository)
    {
        _matchRepository = matchRepository;
        _participantRepository = participantRepository;
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result> Handle(ApproveSharedRideByDriverCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);

        if (match is null || match.Status != SharedRideMatchStatus.WaitingForDriverApproval)
        {
            return Result.Failure(NotWaitingForDriverError, ErrorType.Conflict);
        }

        var driver = await _driverRepository.GetByUserIdAsync(command.RequestingUserId, cancellationToken);

        if (driver is null)
        {
            return Result.Failure(DriverNotFoundError, ErrorType.NotFound);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        var participants = await _participantRepository.GetForMatchAsync(match.Id, cancellationToken);
        var rides = new List<Domain.Rides.Entities.Ride>();

        foreach (var participant in participants)
        {
            var ride = await _rideRepository.GetByIdAsync(participant.RideId, cancellationToken);

            if (ride is null)
            {
                return Result.Failure(RideNoLongerAvailableError, ErrorType.Conflict);
            }

            rides.Add(ride);
        }

        var combinedPassengerCount = rides.Sum(r => r.PassengerCount);

        if (vehicle.SeatCount < combinedPassengerCount)
        {
            return Result.Failure(InsufficientCapacityError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var confirmed = await _matchRepository.TryConfirmWithDriverAsync(match.Id, driver.Id, utcNow, cancellationToken);

        if (!confirmed)
        {
            return Result.Failure(NotWaitingForDriverError, ErrorType.Conflict);
        }

        foreach (var ride in rides)
        {
            var rideConfirmed = await _rideRepository.TryConfirmSharedRideDriverAsync(ride.Id, driver.Id, vehicle.Id, utcNow, cancellationToken);

            if (!rideConfirmed)
            {
                // One of the paired Rides moved on independently in the meantime — surface but do not roll back the Match itself.
                return Result.Failure(RideNoLongerAvailableError, ErrorType.Conflict);
            }
        }

        return Result.Success();
    }
}
