using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideConversationRepository
{
    Task AddAsync(RideConversation conversation, CancellationToken cancellationToken);

    Task<RideConversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<RideConversation?> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<bool> TryMarkReadOnlyAsync(Guid conversationId, DateTime utcNow, CancellationToken cancellationToken);
}
