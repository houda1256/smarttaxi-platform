using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface ISharedRideMatchRepository
{
    Task AddAsync(SharedRideMatch match, CancellationToken cancellationToken);

    Task<SharedRideMatch?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken);

    Task<bool> TryMoveToWaitingForCustomerApprovalsAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>Once both Customer participants have approved.</summary>
    Task<bool> TryMoveToWaitingForDriverApprovalAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>The Driver's explicit acceptance — sets DriverId and confirms in one step (the Driver's acceptance IS the approval, there is no separate "await" state after it).</summary>
    Task<bool> TryConfirmWithDriverAsync(Guid matchId, Guid driverId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryExpireAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid matchId, DateTime utcNow, CancellationToken cancellationToken);
}
