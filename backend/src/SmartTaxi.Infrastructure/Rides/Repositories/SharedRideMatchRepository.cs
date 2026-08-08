using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class SharedRideMatchRepository : ISharedRideMatchRepository
{
    private readonly ApplicationDbContext _context;

    public SharedRideMatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SharedRideMatch match, CancellationToken cancellationToken)
    {
        await _context.SharedRideMatches.AddAsync(match, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<SharedRideMatch?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken) =>
        _context.SharedRideMatches.FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public Task<bool> TryMoveToWaitingForCustomerApprovalsAsync(Guid matchId, CancellationToken cancellationToken) =>
        TryTransitionAsync(matchId, SharedRideMatchStatus.Matching, SharedRideMatchStatus.WaitingForCustomerApprovals, null, cancellationToken);

    public Task<bool> TryMoveToWaitingForDriverApprovalAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(matchId, SharedRideMatchStatus.WaitingForCustomerApprovals, SharedRideMatchStatus.WaitingForDriverApproval, null, cancellationToken);

    public async Task<bool> TryConfirmWithDriverAsync(Guid matchId, Guid driverId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SharedRideMatches
            .Where(match => match.Id == matchId && match.Status == SharedRideMatchStatus.WaitingForDriverApproval)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(match => match.Status, SharedRideMatchStatus.Confirmed)
                .SetProperty(match => match.DriverId, driverId)
                .SetProperty(match => match.ConfirmedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task<bool> TryRejectAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionFromNonTerminalAsync(matchId, SharedRideMatchStatus.Rejected, cancellationToken);

    public Task<bool> TryExpireAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionFromNonTerminalAsync(matchId, SharedRideMatchStatus.Expired, cancellationToken);

    public Task<bool> TryCancelAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionFromNonTerminalAsync(matchId, SharedRideMatchStatus.Cancelled, cancellationToken);

    private async Task<bool> TryTransitionAsync(
        Guid matchId, SharedRideMatchStatus from, SharedRideMatchStatus to, DateTime? utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SharedRideMatches
            .Where(match => match.Id == matchId && match.Status == from)
            .ExecuteUpdateAsync(setters => setters.SetProperty(match => match.Status, to), cancellationToken);

        return rows == 1;
    }

    private static readonly SharedRideMatchStatus[] TerminalStatuses =
        [SharedRideMatchStatus.Confirmed, SharedRideMatchStatus.Rejected, SharedRideMatchStatus.Expired, SharedRideMatchStatus.Cancelled];

    private async Task<bool> TryTransitionFromNonTerminalAsync(Guid matchId, SharedRideMatchStatus to, CancellationToken cancellationToken)
    {
        var rows = await _context.SharedRideMatches
            .Where(match => match.Id == matchId && !TerminalStatuses.Contains(match.Status))
            .ExecuteUpdateAsync(setters => setters.SetProperty(match => match.Status, to), cancellationToken);

        return rows == 1;
    }
}
