using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Support.Repositories;

internal sealed class SupportTicketRepository : ISupportTicketRepository
{
    private readonly ApplicationDbContext _context;

    public SupportTicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        await _context.SupportTickets.AddAsync(ticket, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(ticket).State = EntityState.Detached;
            return false;
        }
    }

    public Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken) =>
        _context.SupportTickets.FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public async Task<PagedResult<SupportTicket>> GetForRequesterAsync(
        Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await ToPagedResultAsync(
            _context.SupportTickets.Where(ticket => ticket.RequesterUserId == requesterUserId), pageNumber, pageSize, cancellationToken);

    public async Task<PagedResult<SupportTicket>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await ToPagedResultAsync(_context.SupportTickets, pageNumber, pageSize, cancellationToken);

    public async Task<bool> TryAssignAsync(Guid ticketId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportTickets
            .Where(ticket => ticket.Id == ticketId && ticket.Status == SupportTicketStatus.Open && ticket.AssignedAdminUserId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.Status, SupportTicketStatus.Assigned)
                .SetProperty(ticket => ticket.AssignedAdminUserId, adminUserId)
                .SetProperty(ticket => ticket.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryReassignAsync(Guid ticketId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportTickets
            .Where(ticket => ticket.Id == ticketId && ticket.Status != SupportTicketStatus.Closed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.AssignedAdminUserId, newAdminUserId)
                .SetProperty(ticket => ticket.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryTransitionAsync(
        Guid ticketId, IReadOnlyCollection<SupportTicketStatus> allowedFromStatuses, SupportTicketStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportTickets
            .Where(ticket => ticket.Id == ticketId && allowedFromStatuses.Contains(ticket.Status)
                && (requiredAdminUserId == null || ticket.AssignedAdminUserId == requiredAdminUserId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.Status, newStatus)
                .SetProperty(ticket => ticket.UpdatedAtUtc, utcNow)
                .SetProperty(
                    ticket => ticket.Resolution,
                    ticket => newStatus == SupportTicketStatus.Resolved ? resolution : ticket.Resolution)
                .SetProperty(
                    ticket => ticket.ResolvedAtUtc,
                    ticket => newStatus == SupportTicketStatus.Resolved ? utcNow : ticket.ResolvedAtUtc)
                .SetProperty(
                    ticket => ticket.ClosedAtUtc,
                    ticket => newStatus == SupportTicketStatus.Closed ? utcNow : ticket.ClosedAtUtc)
                .SetProperty(
                    ticket => ticket.ReopenedAtUtc,
                    ticket => newStatus == SupportTicketStatus.Reopened ? utcNow : ticket.ReopenedAtUtc),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryAddRequesterMessageAndAdvanceAsync(
        Guid ticketId, Guid requesterUserId, string body, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null || ticket.RequesterUserId != requesterUserId || ticket.Status == SupportTicketStatus.Closed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var message = SupportTicketMessage.Create(ticketId, requesterUserId, body, isInternalNote: false, utcNow);
        await _context.SupportTicketMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (ticket.Status == SupportTicketStatus.WaitingForCustomer)
        {
            await _context.SupportTickets
                .Where(t => t.Id == ticketId && t.Status == SupportTicketStatus.WaitingForCustomer)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Status, SupportTicketStatus.InProgress)
                    .SetProperty(t => t.UpdatedAtUtc, utcNow), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TrySetEscalatedIncidentIdAsync(
        Guid ticketId, Guid incidentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportTickets
            .Where(ticket => ticket.Id == ticketId && ticket.EscalatedIncidentId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.EscalatedIncidentId, incidentId)
                .SetProperty(ticket => ticket.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    private static async Task<PagedResult<SupportTicket>> ToPagedResultAsync(
        IQueryable<SupportTicket> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SupportTicket>(items, totalCount, pageNumber, pageSize);
    }
}
