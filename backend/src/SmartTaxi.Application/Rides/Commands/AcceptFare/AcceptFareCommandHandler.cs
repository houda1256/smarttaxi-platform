using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.AcceptFare;

/// <summary>Once accepted, the fare becomes immutable — see Ride.NegotiatedFinalFare, applied here and used directly by CompleteRide instead of the standard calculator.</summary>
public sealed class AcceptFareCommandHandler : ICommandHandler<AcceptFareCommand, Result<decimal>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls le client et le chauffeur sélectionné peuvent négocier.";
    private const string NoActiveNegotiationError = "Aucune négociation active pour cette course.";
    private const string OwnProposalError = "Vous ne pouvez pas accepter votre propre offre.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideFareProposalRepository _proposalRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public AcceptFareCommandHandler(
        IRideRepository rideRepository, IRideFareProposalRepository proposalRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _proposalRepository = proposalRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result<decimal>> Handle(AcceptFareCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isCustomer = ride.CustomerId == command.RequestingUserId;
        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
        var isDriver = driver is not null && driver.UserId == command.RequestingUserId;

        if (!isCustomer && !isDriver)
        {
            return Result<decimal>.Failure(NotParticipantError, ErrorType.NotFound);
        }

        var latest = await _proposalRepository.GetLatestForRideAsync(ride.Id, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (latest is null || latest.Status is not (FareProposalStatus.Proposed or FareProposalStatus.CounterProposed))
        {
            return Result<decimal>.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        if (latest.IsExpired(utcNow))
        {
            await _proposalRepository.TryExpireAsync(latest.Id, utcNow, cancellationToken);
            return Result<decimal>.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        if (latest.ProposedBy == command.RequestingUserId)
        {
            return Result<decimal>.Failure(OwnProposalError, ErrorType.Validation);
        }

        var accepted = await _proposalRepository.TryAcceptAsync(latest.Id, utcNow, cancellationToken);

        if (!accepted)
        {
            return Result<decimal>.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        await _rideRepository.TryApplyNegotiatedFareAsync(ride.Id, latest.Amount, utcNow, cancellationToken);

        return Result<decimal>.Success(latest.Amount);
    }
}
