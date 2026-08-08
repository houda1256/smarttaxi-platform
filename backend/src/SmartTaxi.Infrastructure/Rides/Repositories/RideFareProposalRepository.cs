using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideFareProposalRepository : IRideFareProposalRepository
{
    private static readonly FareProposalStatus[] LiveStatuses = [FareProposalStatus.Proposed, FareProposalStatus.CounterProposed];

    private readonly ApplicationDbContext _context;

    public RideFareProposalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideFareProposal proposal, CancellationToken cancellationToken)
    {
        await _context.RideFareProposals.AddAsync(proposal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAddCounterProposalAsync(
        RideFareProposal counterProposal, Guid previousProposalId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var previousIsStillLive = await _context.RideFareProposals
            .AnyAsync(proposal => proposal.Id == previousProposalId && LiveStatuses.Contains(proposal.Status), cancellationToken);

        if (!previousIsStillLive)
        {
            return false;
        }

        await _context.RideFareProposals.AddAsync(counterProposal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<RideFareProposal?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken) =>
        _context.RideFareProposals.FirstOrDefaultAsync(proposal => proposal.Id == proposalId, cancellationToken);

    public Task<RideFareProposal?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.RideFareProposals
            .Where(proposal => proposal.RideId == rideId)
            .OrderByDescending(proposal => proposal.RoundNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RideFareProposal>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideFareProposals
            .Where(proposal => proposal.RideId == rideId)
            .OrderBy(proposal => proposal.RoundNumber)
            .ToListAsync(cancellationToken);

    public Task<bool> TryAcceptAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(proposalId, FareProposalStatus.Accepted, cancellationToken);

    public Task<bool> TryRejectAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(proposalId, FareProposalStatus.Rejected, cancellationToken);

    public Task<bool> TryExpireAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(proposalId, FareProposalStatus.Expired, cancellationToken);

    /// <summary>
    /// Guarded by the proposal's own Status being live AND no newer round
    /// existing for the same Ride — a stale Accept/Reject on a superseded
    /// round must fail, never silently succeed.
    /// </summary>
    private async Task<bool> TryTransitionAsync(Guid proposalId, FareProposalStatus to, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var proposal = await _context.RideFareProposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

        if (proposal is null || !LiveStatuses.Contains(proposal.Status))
        {
            return false;
        }

        var hasNewerRound = await _context.RideFareProposals
            .AnyAsync(p => p.RideId == proposal.RideId && p.RoundNumber > proposal.RoundNumber, cancellationToken);

        if (hasNewerRound)
        {
            return false;
        }

        var rows = await _context.RideFareProposals
            .Where(p => p.Id == proposalId && LiveStatuses.Contains(p.Status))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, to), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
