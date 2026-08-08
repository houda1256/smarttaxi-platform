using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSharedRideMatchRepository : ISharedRideMatchRepository
{
    private readonly Dictionary<Guid, SharedRideMatch> _matchesById = new();

    public Task AddAsync(SharedRideMatch match, CancellationToken cancellationToken)
    {
        _matchesById[match.Id] = match;
        return Task.CompletedTask;
    }

    public Task<SharedRideMatch?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matchesById.GetValueOrDefault(matchId));

    public Task<bool> TryMoveToWaitingForCustomerApprovalsAsync(Guid matchId, CancellationToken cancellationToken) =>
        TryTransition(matchId, SharedRideMatchStatus.Matching, SharedRideMatchStatus.WaitingForCustomerApprovals);

    public Task<bool> TryMoveToWaitingForDriverApprovalAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(matchId, SharedRideMatchStatus.WaitingForCustomerApprovals, SharedRideMatchStatus.WaitingForDriverApproval);

    public Task<bool> TryConfirmWithDriverAsync(Guid matchId, Guid driverId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_matchesById.TryGetValue(matchId, out var match) || match.Status != SharedRideMatchStatus.WaitingForDriverApproval)
        {
            return Task.FromResult(false);
        }

        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.DriverId))!.SetValue(match, driverId);
        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.Status))!.SetValue(match, SharedRideMatchStatus.Confirmed);
        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.ConfirmedAt))!.SetValue(match, utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_matchesById.TryGetValue(matchId, out var match)
            || match.Status is SharedRideMatchStatus.Confirmed or SharedRideMatchStatus.Rejected or SharedRideMatchStatus.Cancelled)
        {
            return Task.FromResult(false);
        }

        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.Status))!.SetValue(match, SharedRideMatchStatus.Rejected);
        return Task.FromResult(true);
    }

    public Task<bool> TryExpireAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionFromAny(matchId, SharedRideMatchStatus.Expired);

    public Task<bool> TryCancelAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionFromAny(matchId, SharedRideMatchStatus.Cancelled);

    private Task<bool> TryTransition(Guid matchId, SharedRideMatchStatus from, SharedRideMatchStatus to)
    {
        if (!_matchesById.TryGetValue(matchId, out var match) || match.Status != from)
        {
            return Task.FromResult(false);
        }

        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.Status))!.SetValue(match, to);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransitionFromAny(Guid matchId, SharedRideMatchStatus to)
    {
        if (!_matchesById.TryGetValue(matchId, out var match)
            || match.Status is SharedRideMatchStatus.Confirmed or SharedRideMatchStatus.Rejected or SharedRideMatchStatus.Cancelled)
        {
            return Task.FromResult(false);
        }

        typeof(SharedRideMatch).GetProperty(nameof(SharedRideMatch.Status))!.SetValue(match, to);
        return Task.FromResult(true);
    }
}
