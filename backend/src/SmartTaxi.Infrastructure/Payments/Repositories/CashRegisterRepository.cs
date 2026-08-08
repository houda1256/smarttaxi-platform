using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class CashRegisterRepository : ICashRegisterRepository
{
    private readonly ApplicationDbContext _context;

    public CashRegisterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CashRegisterBox cashRegister, CancellationToken cancellationToken)
    {
        await _context.CashRegisters.AddAsync(cashRegister, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<CashRegisterBox?> GetByIdAsync(Guid cashRegisterId, CancellationToken cancellationToken) =>
        _context.CashRegisters.FirstOrDefaultAsync(cashRegister => cashRegister.Id == cashRegisterId, cancellationToken);

    public async Task<IReadOnlyCollection<CashRegisterBox>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _context.CashRegisters
            .Where(cashRegister => cashRegister.OwnerId == ownerId)
            .OrderBy(cashRegister => cashRegister.CreatedAt)
            .ToListAsync(cancellationToken);
}
