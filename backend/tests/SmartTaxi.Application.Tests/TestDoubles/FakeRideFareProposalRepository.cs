using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideFareProposalRepository : IRideFareProposalRepository
{
    private readonly Dictionary<Guid, RideFareProposal> _proposalsById = new();

    private static readonly FareProposalStatus[] LiveStatuses = [FareProposalStatus.Proposed, FareProposalStatus.CounterProposed];

    public Task AddAsync(RideFareProposal proposal, CancellationToken cancellationToken)
    {
        _proposalsById[proposal.Id] = proposal;
        return Task.CompletedTask;
    }

    public Task<bool> TryAddCounterProposalAsync(RideFareProposal counterProposal, Guid previousProposalId, CancellationToken cancellationToken)
    {
        if (!_proposalsById.TryGetValue(previousProposalId, out var previous) || !LiveStatuses.Contains(previous.Status))
        {
            return Task.FromResult(false);
        }

        _proposalsById[counterProposal.Id] = counterProposal;
        return Task.FromResult(true);
    }

    public Task<RideFareProposal?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken) =>
        Task.FromResult(_proposalsById.GetValueOrDefault(proposalId));

    public Task<RideFareProposal?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var latest = _proposalsById.Values
            .Where(p => p.RideId == rideId)
            .OrderByDescending(p => p.RoundNumber)
            .FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<IReadOnlyCollection<RideFareProposal>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideFareProposal> proposals = _proposalsById.Values.Where(p => p.RideId == rideId).ToList();
        return Task.FromResult(proposals);
    }

    public Task<bool> TryAcceptAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryResolve(proposalId, FareProposalStatus.Accepted);

    public Task<bool> TryRejectAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryResolve(proposalId, FareProposalStatus.Rejected);

    public Task<bool> TryExpireAsync(Guid proposalId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryResolve(proposalId, FareProposalStatus.Expired);

    private Task<bool> TryResolve(Guid proposalId, FareProposalStatus to)
    {
        if (!_proposalsById.TryGetValue(proposalId, out var proposal) || !LiveStatuses.Contains(proposal.Status))
        {
            return Task.FromResult(false);
        }

        var hasNewerRound = _proposalsById.Values.Any(p => p.RideId == proposal.RideId && p.RoundNumber > proposal.RoundNumber);

        if (hasNewerRound)
        {
            return Task.FromResult(false);
        }

        typeof(RideFareProposal).GetProperty(nameof(RideFareProposal.Status))!.SetValue(proposal, to);
        return Task.FromResult(true);
    }
}
