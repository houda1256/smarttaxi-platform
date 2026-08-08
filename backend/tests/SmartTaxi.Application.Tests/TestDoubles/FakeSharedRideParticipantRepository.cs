using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSharedRideParticipantRepository : ISharedRideParticipantRepository
{
    private readonly Dictionary<Guid, SharedRideParticipant> _participantsById = new();

    public Task AddAsync(SharedRideParticipant participant, CancellationToken cancellationToken)
    {
        _participantsById[participant.Id] = participant;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SharedRideParticipant>> GetForMatchAsync(Guid sharedRideMatchId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SharedRideParticipant> participants =
            _participantsById.Values.Where(p => p.SharedRideMatchId == sharedRideMatchId).ToList();
        return Task.FromResult(participants);
    }

    public Task<SharedRideParticipant?> GetForMatchAndRideAsync(Guid sharedRideMatchId, Guid rideId, CancellationToken cancellationToken)
    {
        var participant = _participantsById.Values.FirstOrDefault(p => p.SharedRideMatchId == sharedRideMatchId && p.RideId == rideId);
        return Task.FromResult(participant);
    }

    public Task<SharedRideParticipant?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var participant = _participantsById.Values.FirstOrDefault(p =>
            p.RideId == rideId && p.ApprovalStatus != SharedRideParticipantApprovalStatus.Rejected);
        return Task.FromResult(participant);
    }

    public Task<bool> TryApproveAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(participantId, SharedRideParticipantApprovalStatus.Approved, utcNow);

    public Task<bool> TryRejectAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(participantId, SharedRideParticipantApprovalStatus.Rejected, utcNow);

    private Task<bool> TryTransition(Guid participantId, SharedRideParticipantApprovalStatus to, DateTime utcNow)
    {
        if (!_participantsById.TryGetValue(participantId, out var participant)
            || participant.ApprovalStatus != SharedRideParticipantApprovalStatus.Pending)
        {
            return Task.FromResult(false);
        }

        typeof(SharedRideParticipant).GetProperty(nameof(SharedRideParticipant.ApprovalStatus))!.SetValue(participant, to);
        typeof(SharedRideParticipant).GetProperty(nameof(SharedRideParticipant.RespondedAt))!.SetValue(participant, utcNow);
        return Task.FromResult(true);
    }
}
