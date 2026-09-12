using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideMessageRepository
{
    Task AddAsync(RideMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideMessage>> GetForConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<bool> TryReportAsync(Guid messageId, CancellationToken cancellationToken);
}
