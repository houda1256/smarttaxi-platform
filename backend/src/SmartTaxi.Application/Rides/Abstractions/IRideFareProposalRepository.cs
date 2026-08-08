using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideFareProposalRepository
{
    /// <summary>Round 1 only — no previous row to guard against.</summary>
    Task AddAsync(RideFareProposal proposal, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically re-checks that `previousProposalId` is still the live
    /// (Proposed/CounterProposed) row before inserting `counterProposal` —
    /// this is what makes "concurrent acceptance and counter-offer cannot
    /// both succeed" true: if the previous row was accepted/rejected first,
    /// this call fails and no counter-proposal row is ever created.
    /// </summary>
    Task<bool> TryAddCounterProposalAsync(RideFareProposal counterProposal, Guid previousProposalId, CancellationToken cancellationToken);

    Task<RideFareProposal?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken);

    Task<RideFareProposal?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideFareProposal>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    /// <summary>
    /// Guarded by both the proposal's own Status being live AND no newer
    /// round existing for the same Ride — accepting a proposal that has just
    /// been superseded by a fresh counter-offer must fail, not silently
    /// succeed on stale data.
    /// </summary>
    Task<bool> TryAcceptAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryExpireAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken);
}
