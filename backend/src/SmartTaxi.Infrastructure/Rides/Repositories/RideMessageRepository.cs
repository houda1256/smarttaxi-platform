using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideMessageRepository : IRideMessageRepository
{
    private readonly ApplicationDbContext _context;

    public RideMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideMessage message, CancellationToken cancellationToken)
    {
        await _context.RideMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RideMessage>> GetForConversationAsync(Guid conversationId, CancellationToken cancellationToken) =>
        await _context.RideMessages
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.SentAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryReportAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var rows = await _context.RideMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(message => message.IsReported, true), cancellationToken);

        return rows == 1;
    }
}
