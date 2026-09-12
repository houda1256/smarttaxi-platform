using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeCashMovementRepository : ICashMovementRepository
{
    private readonly List<CashMovement> _movements = [];

    public Task AddAsync(CashMovement movement, CancellationToken cancellationToken)
    {
        _movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CashMovement>> GetForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CashMovement> movements = _movements.Where(m => m.CashRegisterSessionId == cashRegisterSessionId).ToList();
        return Task.FromResult(movements);
    }

    public Task<decimal> GetNetTotalForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken) =>
        Task.FromResult(_movements.Where(m => m.CashRegisterSessionId == cashRegisterSessionId).Sum(m => m.Amount));
}
