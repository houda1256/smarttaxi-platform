using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CounterProposeFare;

/// <summary>
/// Only the party who did NOT make the current live proposal may counter it
/// — this is what makes negotiation alternate turns. The repository-level
/// atomic guard (re-checking the previous row is still live before inserting)
/// is what makes "concurrent acceptance and counter-offer cannot both
/// succeed" true.
/// </summary>
public sealed class CounterProposeFareCommandHandler : ICommandHandler<CounterProposeFareCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls le client et le chauffeur sélectionné peuvent négocier.";
    private const string NoActiveNegotiationError = "Aucune négociation active pour cette course.";
    private const string SameProposerError = "Vous ne pouvez pas contre-proposer votre propre offre.";
    private const string MaxRoundsReachedError = "Le nombre maximal de rounds de négociation est atteint.";
    private const string ConcurrentResolutionError = "Cette proposition vient d'être acceptée, rejetée ou expirée.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideFareProposalRepository _proposalRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly INegotiationPolicy _negotiationPolicy;

    public CounterProposeFareCommandHandler(
        IRideRepository rideRepository, IRideFareProposalRepository proposalRepository, IDriverProfileRepository driverRepository,
        INegotiationPolicy negotiationPolicy)
    {
        _rideRepository = rideRepository;
        _proposalRepository = proposalRepository;
        _driverRepository = driverRepository;
        _negotiationPolicy = negotiationPolicy;
    }

    public async Task<Result<Guid>> Handle(CounterProposeFareCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isCustomer = ride.CustomerId == command.RequestingUserId;
        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
        var isDriver = driver is not null && driver.UserId == command.RequestingUserId;

        if (!isCustomer && !isDriver)
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.NotFound);
        }

        var latest = await _proposalRepository.GetLatestForRideAsync(ride.Id, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (latest is null || latest.Status is not (FareProposalStatus.Proposed or FareProposalStatus.CounterProposed))
        {
            return Result<Guid>.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        if (latest.IsExpired(utcNow))
        {
            await _proposalRepository.TryExpireAsync(latest.Id, utcNow, cancellationToken);
            return Result<Guid>.Failure(NoActiveNegotiationError, ErrorType.Conflict);
        }

        if (latest.ProposedBy == command.RequestingUserId)
        {
            return Result<Guid>.Failure(SameProposerError, ErrorType.Validation);
        }

        var nextRound = latest.RoundNumber + 1;

        if (nextRound > _negotiationPolicy.MaxNegotiationRounds)
        {
            return Result<Guid>.Failure(MaxRoundsReachedError, ErrorType.Conflict);
        }

        RideFareProposal counterProposal;

        try
        {
            counterProposal = RideFareProposal.CreateCounterProposal(
                ride.Id, command.RequestingUserId, command.Amount, ride.Currency, nextRound, utcNow,
                TimeSpan.FromMinutes(_negotiationPolicy.FareProposalExpiryMinutes));
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _proposalRepository.TryAddCounterProposalAsync(counterProposal, latest.Id, cancellationToken);

        if (!added)
        {
            return Result<Guid>.Failure(ConcurrentResolutionError, ErrorType.Conflict);
        }

        return Result<Guid>.Success(counterProposal.Id);
    }
}
