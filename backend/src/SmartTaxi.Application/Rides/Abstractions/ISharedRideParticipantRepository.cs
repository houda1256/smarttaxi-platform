using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface ISharedRideParticipantRepository
{
    Task AddAsync(SharedRideParticipant participant, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SharedRideParticipant>> GetForMatchAsync(Guid sharedRideMatchId, CancellationToken cancellationToken);

    Task<SharedRideParticipant?> GetForMatchAndRideAsync(Guid sharedRideMatchId, Guid rideId, CancellationToken cancellationToken);

    /// <summary>Any (non-rejected/expired) match this Ride currently participates in — used to exclude already-matched Rides from new candidate searches.</summary>
    Task<SharedRideParticipant?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid participantId, DateTime utcNow, CancellationToken cancellationToken);
}
