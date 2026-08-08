using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.RejectFare;

public sealed class RejectFareCommandHandler : ICommandHandler<RejectFareCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls le client et le chauffeur sélectionné peuvent négocier.";
    private const string NoActiveNegotiationError = "Aucune négociation active pour cette course.";
    private const string OwnProposalError = "Vous ne pouvez pas rejeter votre propre offre.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideFareProposalRepository _proposalRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public RejectFareCommandHandler(
        IRideRepository rideRepository, IRideFareProposalRepository proposalRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _proposalRepository = proposalRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(RejectFareCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isCustomer = ride.CustomerId == command.RequestingUserId;
        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
        var isDriver = driver is not null && driver.UserId == command.RequestingUserId;

        if (!isCustomer && !isDriver)
        {
            return Result.Failure(NotParticipantError, ErrorType.NotFound);
        }

        var latest = await _proposalRepository.GetLatestForRideAsync(ride.Id, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (latest is null || latest.Status is not (FareProposalStatus.Proposed or FareProposalStatus.CounterProposed))
        {
            return Result.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        if (latest.ProposedBy == command.RequestingUserId)
        {
            return Result.Failure(OwnProposalError, ErrorType.Validation);
        }

        var rejected = await _proposalRepository.TryRejectAsync(latest.Id, utcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
