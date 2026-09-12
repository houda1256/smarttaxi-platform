using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// One immutable row per negotiation round — a counter-offer is a new row,
/// never an edit of the previous one, so the full negotiation history is
/// preserved for free. Accept/Reject/Expire are atomic repository-level
/// guards against the latest row, same pattern as every Fleet transition.
/// </summary>
public sealed class RideFareProposal : AggregateRoot
{
    public Guid RideId { get; private set; }
    public Guid ProposedBy { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public int RoundNumber { get; private set; }
    public FareProposalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private RideFareProposal()
    {
    }

    private RideFareProposal(
        Guid rideId, Guid proposedBy, decimal amount, string currency, int roundNumber, FareProposalStatus status,
        DateTime utcNow, TimeSpan expiry)
        : base(Guid.NewGuid())
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Le montant proposé doit être positif.");
        }

        if (roundNumber <= 0)
        {
            throw new ArgumentException("Le numéro de round doit être positif.");
        }

        RideId = rideId;
        ProposedBy = proposedBy;
        Amount = amount;
        Currency = currency;
        RoundNumber = roundNumber;
        Status = status;
        CreatedAt = utcNow;
        ExpiresAt = utcNow.Add(expiry);
    }

    public static RideFareProposal CreateInitialProposal(
        Guid rideId, Guid proposedBy, decimal amount, string currency, DateTime utcNow, TimeSpan expiry)
    {
        var proposal = new RideFareProposal(rideId, proposedBy, amount, currency, roundNumber: 1, FareProposalStatus.Proposed, utcNow, expiry);
        proposal.RaiseDomainEvent(new FareProposed(proposal.Id, rideId, proposedBy, amount, utcNow));
        return proposal;
    }

    public static RideFareProposal CreateCounterProposal(
        Guid rideId, Guid proposedBy, decimal amount, string currency, int roundNumber, DateTime utcNow, TimeSpan expiry)
    {
        if (roundNumber <= 1)
        {
            throw new ArgumentException("Une contre-proposition doit avoir un numéro de round supérieur à 1.");
        }

        var proposal = new RideFareProposal(rideId, proposedBy, amount, currency, roundNumber, FareProposalStatus.CounterProposed, utcNow, expiry);
        proposal.RaiseDomainEvent(new FareCounterProposed(proposal.Id, rideId, proposedBy, amount, roundNumber, utcNow));
        return proposal;
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
}
