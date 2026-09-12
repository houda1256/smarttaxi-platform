using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.ProposeFare;

/// <summary>Opens a negotiation — always initiated by the Customer, only ever valid on a RideType.Negotiated Ride that has a selected Driver.</summary>
public sealed class ProposeFareCommandHandler : ICommandHandler<ProposeFareCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotNegotiatedError = "Cette course ne supporte pas la négociation de tarif.";
    private const string NoDriverSelectedError = "Aucun chauffeur n'est encore sélectionné pour cette course.";
    private const string NegotiationInProgressError = "Une négociation est déjà en cours pour cette course.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideFareProposalRepository _proposalRepository;
    private readonly INegotiationPolicy _negotiationPolicy;

    public ProposeFareCommandHandler(
        IRideRepository rideRepository, IRideFareProposalRepository proposalRepository, INegotiationPolicy negotiationPolicy)
    {
        _rideRepository = rideRepository;
        _proposalRepository = proposalRepository;
        _negotiationPolicy = negotiationPolicy;
    }

    public async Task<Result<Guid>> Handle(ProposeFareCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.RideType != RideType.Negotiated)
        {
            return Result<Guid>.Failure(NotNegotiatedError, ErrorType.Validation);
        }

        if (ride.SelectedDriverId is null)
        {
            return Result<Guid>.Failure(NoDriverSelectedError, ErrorType.Conflict);
        }

        var latest = await _proposalRepository.GetLatestForRideAsync(ride.Id, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (latest is not null && !latest.IsExpired(utcNow) && latest.Status is FareProposalStatus.Proposed or FareProposalStatus.CounterProposed)
        {
            return Result<Guid>.Failure(NegotiationInProgressError, ErrorType.Conflict);
        }

        RideFareProposal proposal;

        try
        {
            proposal = RideFareProposal.CreateInitialProposal(
                ride.Id, command.RequestingUserId, command.Amount, ride.Currency, utcNow,
                TimeSpan.FromMinutes(_negotiationPolicy.FareProposalExpiryMinutes));
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _proposalRepository.AddAsync(proposal, cancellationToken);

        return Result<Guid>.Success(proposal.Id);
    }
}
