using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeCashRegisterRepository : ICashRegisterRepository
{
    private readonly Dictionary<Guid, CashRegisterBox> _registersById = new();

    public Task AddAsync(CashRegisterBox cashRegister, CancellationToken cancellationToken)
    {
        _registersById[cashRegister.Id] = cashRegister;
        return Task.CompletedTask;
    }

    public Task<CashRegisterBox?> GetByIdAsync(Guid cashRegisterId, CancellationToken cancellationToken) =>
        Task.FromResult(_registersById.GetValueOrDefault(cashRegisterId));

    public Task<IReadOnlyCollection<CashRegisterBox>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CashRegisterBox> registers = _registersById.Values.Where(r => r.OwnerId == ownerId).ToList();
        return Task.FromResult(registers);
    }
}
