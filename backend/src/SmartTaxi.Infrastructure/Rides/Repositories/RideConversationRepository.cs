using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideConversationRepository : IRideConversationRepository
{
    private readonly ApplicationDbContext _context;

    public RideConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideConversation conversation, CancellationToken cancellationToken)
    {
        await _context.RideConversations.AddAsync(conversation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RideConversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken) =>
        _context.RideConversations.FirstOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);

    public Task<RideConversation?> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.RideConversations.FirstOrDefaultAsync(conversation => conversation.RideId == rideId, cancellationToken);

    public async Task<bool> TryMarkReadOnlyAsync(Guid conversationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RideConversations
            .Where(conversation => conversation.Id == conversationId && conversation.Status == RideConversationStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(conversation => conversation.Status, RideConversationStatus.ReadOnly)
                .SetProperty(conversation => conversation.ReadOnlyAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
