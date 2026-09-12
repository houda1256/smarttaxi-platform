using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Abstractions;

/// <summary>Append-only — no update/delete method exists on purpose (the master prompt's "never physically delete financial records" rule).</summary>
public interface ICashMovementRepository
{
    Task AddAsync(CashMovement movement, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CashMovement>> GetForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken);

    Task<decimal> GetNetTotalForSessionAsync(Guid cashRegisterSessionId, CancellationToken cancellationToken);
}
