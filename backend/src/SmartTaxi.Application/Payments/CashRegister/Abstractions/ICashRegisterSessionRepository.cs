using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Payments.CashRegister.Abstractions;

public interface ICashRegisterSessionRepository
{
    /// <summary>May return false if an Open session already exists for this register — see the DB partial unique index on (CashRegisterId) WHERE Status = 'Open' (only one Open session per register at a time).</summary>
    Task<bool> TryAddAsync(CashRegisterSession session, CancellationToken cancellationToken);

    Task<CashRegisterSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<CashRegisterSession?> GetOpenForRegisterAsync(Guid cashRegisterId, CancellationToken cancellationToken);

    Task<PagedResult<CashRegisterSession>> GetForOwnerAsync(
        Guid ownerId, CashRegisterSessionStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Guarded by Status = Open. Every field (expected/actual/difference/reason) is written atomically alongside the status change.</summary>
    Task<bool> TryCloseAsync(
        Guid sessionId, decimal closingExpectedBalance, decimal closingActualBalance, decimal difference,
        string? differenceReason, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryReconcileAsync(Guid sessionId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDisputeAsync(Guid sessionId, string reason, DateTime utcNow, CancellationToken cancellationToken);
}
