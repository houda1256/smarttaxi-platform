using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Support.Repositories;

internal sealed class SupportTicketMessageRepository : ISupportTicketMessageRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SupportTicketMessage message, CancellationToken cancellationToken)
    {
        await _context.SupportTicketMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SupportTicketMessage>> GetVisibleForRequesterAsync(
        Guid ticketId, CancellationToken cancellationToken) =>
        await _context.SupportTicketMessages
            .Where(message => message.TicketId == ticketId && !message.IsInternalNote)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<SupportTicketMessage>> GetAllForAdminAsync(
        Guid ticketId, CancellationToken cancellationToken) =>
        await _context.SupportTicketMessages
            .Where(message => message.TicketId == ticketId)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
