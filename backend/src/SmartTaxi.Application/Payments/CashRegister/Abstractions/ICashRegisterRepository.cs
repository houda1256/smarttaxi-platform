using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Abstractions;

public interface ICashRegisterRepository
{
    Task AddAsync(CashRegisterBox cashRegister, CancellationToken cancellationToken);

    Task<CashRegisterBox?> GetByIdAsync(Guid cashRegisterId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CashRegisterBox>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
}
