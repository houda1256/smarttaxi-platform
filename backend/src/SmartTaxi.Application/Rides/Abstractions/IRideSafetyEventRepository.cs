using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideSafetyEventRepository
{
    Task AddAsync(RideSafetyEvent safetyEvent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideSafetyEvent>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<bool> TryAcknowledgeAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryResolveAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken);
}
