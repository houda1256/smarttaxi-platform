using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideMessageRepository : IRideMessageRepository
{
    private readonly Dictionary<Guid, RideMessage> _messagesById = new();

    public Task AddAsync(RideMessage message, CancellationToken cancellationToken)
    {
        _messagesById[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RideMessage>> GetForConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideMessage> messages = _messagesById.Values
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToList();
        return Task.FromResult(messages);
    }

    public Task<bool> TryReportAsync(Guid messageId, CancellationToken cancellationToken)
    {
        if (!_messagesById.TryGetValue(messageId, out var message))
        {
            return Task.FromResult(false);
        }

        typeof(RideMessage).GetProperty(nameof(RideMessage.IsReported))!.SetValue(message, true);
        return Task.FromResult(true);
    }
}
