using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class CashRegisterSessionRepository : ICashRegisterSessionRepository
{
    private readonly ApplicationDbContext _context;

    public CashRegisterSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(CashRegisterSession session, CancellationToken cancellationToken)
    {
        await _context.CashRegisterSessions.AddAsync(session, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The partial unique index on CashRegisterId (WHERE Status = 'Open') rejected a concurrent second Open session.
            _context.Entry(session).State = EntityState.Detached;
            return false;
        }
    }

    public Task<CashRegisterSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        _context.CashRegisterSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public Task<CashRegisterSession?> GetOpenForRegisterAsync(Guid cashRegisterId, CancellationToken cancellationToken) =>
        _context.CashRegisterSessions.FirstOrDefaultAsync(
            session => session.CashRegisterId == cashRegisterId && session.Status == CashRegisterSessionStatus.Open, cancellationToken);

    public async Task<PagedResult<CashRegisterSession>> GetForOwnerAsync(
        Guid ownerId, CashRegisterSessionStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query =
            from session in _context.CashRegisterSessions
            join cashRegister in _context.CashRegisters on session.CashRegisterId equals cashRegister.Id
            where cashRegister.OwnerId == ownerId
            select session;

        if (status is not null)
        {
            query = query.Where(session => session.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(session => session.OpenedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CashRegisterSession>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TryCloseAsync(
        Guid sessionId, decimal closingExpectedBalance, decimal closingActualBalance, decimal difference,
        string? differenceReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashRegisterSessions
            .Where(session => session.Id == sessionId && session.Status == CashRegisterSessionStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.Status, CashRegisterSessionStatus.Closed)
                .SetProperty(session => session.ClosingExpectedBalance, closingExpectedBalance)
                .SetProperty(session => session.ClosingActualBalance, closingActualBalance)
                .SetProperty(session => session.Difference, difference)
                .SetProperty(session => session.DifferenceReason, differenceReason)
                .SetProperty(session => session.ClosedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryReconcileAsync(Guid sessionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashRegisterSessions
            .Where(session => session.Id == sessionId && session.Status == CashRegisterSessionStatus.Closed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.Status, CashRegisterSessionStatus.Reconciled), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryDisputeAsync(Guid sessionId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashRegisterSessions
            .Where(session => session.Id == sessionId && session.Status == CashRegisterSessionStatus.Closed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.Status, CashRegisterSessionStatus.Disputed)
                .SetProperty(session => session.DifferenceReason, reason), cancellationToken);

        return rows == 1;
    }
}
