using SmartTaxi.Domain.Fleet.Contracts.Entities;

namespace SmartTaxi.Application.Fleet.Contracts.Abstractions;

public interface IDriverOwnerContractRepository
{
    Task AddAsync(DriverOwnerContract contract, CancellationToken cancellationToken);

    Task<DriverOwnerContract?> GetByIdAsync(Guid contractId, CancellationToken cancellationToken);

    /// <summary>The single Active or PendingSignature contract (if any) for a driver-owner pair — used to block duplicates.</summary>
    Task<DriverOwnerContract?> GetActiveOrPendingForDriverOwnerAsync(Guid driverId, Guid ownerId, CancellationToken cancellationToken);

    /// <summary>The strictly Active contract (if any) for a driver-owner pair — added for Payments' per-ride revenue-sharing lookup, which needs the currently-in-force terms, not a pending draft.</summary>
    Task<DriverOwnerContract?> GetActiveForDriverAndOwnerAsync(Guid driverId, Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverOwnerContract>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverOwnerContract>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    Task UpdateAsync(DriverOwnerContract contract, CancellationToken cancellationToken);

    Task<bool> TrySubmitAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryActivateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryTerminateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken);
}
