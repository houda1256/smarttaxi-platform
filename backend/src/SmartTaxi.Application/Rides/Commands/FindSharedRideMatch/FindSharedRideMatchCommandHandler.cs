using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.FindSharedRideMatch;

/// <summary>
/// Trip-compatibility only (pickup distance, request-time window, directional
/// compatibility) — deterministic, rule-based, never ML. Vehicle seat
/// capacity is deliberately checked later, at driver-confirmation time (see
/// ApproveSharedRideByDriver), since no specific vehicle is being considered
/// yet at this matching stage. Returns null (success, no match) rather than
/// an error when no compatible Ride currently exists — absence of a match is
/// a normal outcome, not a failure.
/// </summary>
public sealed class FindSharedRideMatchCommandHandler : ICommandHandler<FindSharedRideMatchCommand, Result<Guid?>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotSharedRideError = "Cette course n'est pas de type partagé.";

    private readonly IRideRepository _rideRepository;
    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly ISharedRideParticipantRepository _participantRepository;
    private readonly ISharedRideMatchingService _matchingService;
    private readonly ISharedRideMatchingPolicy _matchingPolicy;

    public FindSharedRideMatchCommandHandler(
        IRideRepository rideRepository, ISharedRideMatchRepository matchRepository,
        ISharedRideParticipantRepository participantRepository, ISharedRideMatchingService matchingService,
        ISharedRideMatchingPolicy matchingPolicy)
    {
        _rideRepository = rideRepository;
        _matchRepository = matchRepository;
        _participantRepository = participantRepository;
        _matchingService = matchingService;
        _matchingPolicy = matchingPolicy;
    }

    public async Task<Result<Guid?>> Handle(FindSharedRideMatchCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<Guid?>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.RideType != RideType.Shared)
        {
            return Result<Guid?>.Failure(NotSharedRideError, ErrorType.Validation);
        }

        var alreadyMatched = await _participantRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (alreadyMatched is not null)
        {
            return Result<Guid?>.Success(alreadyMatched.SharedRideMatchId);
        }

        var candidates = await _rideRepository.GetPendingSharedRidesAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (candidate.Id == ride.Id || candidate.CustomerId == ride.CustomerId)
            {
                continue;
            }

            var candidateAlreadyMatched = await _participantRepository.GetActiveForRideAsync(candidate.Id, cancellationToken);

            if (candidateAlreadyMatched is not null)
            {
                continue;
            }

            var report = _matchingService.CheckCompatibility(new SharedRideCompatibilityInput(
                ride.PickupLocation, candidate.PickupLocation, ride.DestinationLocation, candidate.DestinationLocation,
                ride.RequestedAt, candidate.RequestedAt, ride.PassengerCount, candidate.PassengerCount,
                VehicleSeatCount: int.MaxValue));

            if (!report.IsCompatible)
            {
                continue;
            }

            var utcNow = DateTime.UtcNow;
            var match = SharedRideMatch.Suggest(utcNow, _matchingPolicy.MatchExpiry);
            await _matchRepository.AddAsync(match, cancellationToken);

            await _participantRepository.AddAsync(new SharedRideParticipant(match.Id, ride.Id, ride.CustomerId, utcNow), cancellationToken);
            await _participantRepository.AddAsync(new SharedRideParticipant(match.Id, candidate.Id, candidate.CustomerId, utcNow), cancellationToken);

            await _matchRepository.TryMoveToWaitingForCustomerApprovalsAsync(match.Id, cancellationToken);

            return Result<Guid?>.Success(match.Id);
        }

        return Result<Guid?>.Success(null);
    }
}
