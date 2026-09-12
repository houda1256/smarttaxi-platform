using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class CashMovementRepository : ICashMovementRepository
{
    private readonly ApplicationDbContext _context;

    public CashMovementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CashMovement movement, CancellationToken cancellationToken)
    {
        await _context.CashMovements.AddAsync(movement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CashMovement>> GetForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken) =>
        await _context.CashMovements
            .Where(movement => movement.CashRegisterSessionId == cashRegisterSessionId)
            .OrderBy(movement => movement.RecordedAt)
            .ToListAsync(cancellationToken);

    public Task<decimal> GetNetTotalForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken) =>
        _context.CashMovements
            .Where(movement => movement.CashRegisterSessionId == cashRegisterSessionId)
            .SumAsync(movement => movement.Amount, cancellationToken);
}
