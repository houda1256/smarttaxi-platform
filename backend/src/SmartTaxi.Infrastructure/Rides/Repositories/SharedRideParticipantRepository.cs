using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class SharedRideParticipantRepository : ISharedRideParticipantRepository
{
    private readonly ApplicationDbContext _context;

    public SharedRideParticipantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SharedRideParticipant participant, CancellationToken cancellationToken)
    {
        await _context.SharedRideParticipants.AddAsync(participant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SharedRideParticipant>> GetForMatchAsync(Guid sharedRideMatchId, CancellationToken cancellationToken) =>
        await _context.SharedRideParticipants
            .Where(participant => participant.SharedRideMatchId == sharedRideMatchId)
            .ToListAsync(cancellationToken);

    public Task<SharedRideParticipant?> GetForMatchAndRideAsync(Guid sharedRideMatchId, Guid rideId, CancellationToken cancellationToken) =>
        _context.SharedRideParticipants.FirstOrDefaultAsync(
            participant => participant.SharedRideMatchId == sharedRideMatchId && participant.RideId == rideId, cancellationToken);

    public Task<SharedRideParticipant?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.SharedRideParticipants
            .Where(participant => participant.RideId == rideId
                && participant.ApprovalStatus != SharedRideParticipantApprovalStatus.Rejected)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> TryApproveAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(participantId, SharedRideParticipantApprovalStatus.Approved, utcNow, cancellationToken);

    public Task<bool> TryRejectAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(participantId, SharedRideParticipantApprovalStatus.Rejected, utcNow, cancellationToken);

    private async Task<bool> TryTransitionAsync(
        Guid participantId, SharedRideParticipantApprovalStatus to, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SharedRideParticipants
            .Where(participant => participant.Id == participantId && participant.ApprovalStatus == SharedRideParticipantApprovalStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(participant => participant.ApprovalStatus, to)
                .SetProperty(participant => participant.RespondedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
