using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideConversationRepository : IRideConversationRepository
{
    private readonly Dictionary<Guid, RideConversation> _conversationsById = new();

    public Task AddAsync(RideConversation conversation, CancellationToken cancellationToken)
    {
        _conversationsById[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task<RideConversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken) =>
        Task.FromResult(_conversationsById.GetValueOrDefault(conversationId));

    public Task<RideConversation?> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var conversation = _conversationsById.Values.FirstOrDefault(c => c.RideId == rideId);
        return Task.FromResult(conversation);
    }

    public Task<bool> TryMarkReadOnlyAsync(Guid conversationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_conversationsById.TryGetValue(conversationId, out var conversation) || conversation.Status != RideConversationStatus.Active)
        {
            return Task.FromResult(false);
        }

        typeof(RideConversation).GetProperty(nameof(RideConversation.Status))!.SetValue(conversation, RideConversationStatus.ReadOnly);
        typeof(RideConversation).GetProperty(nameof(RideConversation.ReadOnlyAt))!.SetValue(conversation, utcNow);
        return Task.FromResult(true);
    }
}
