using SmartTaxi.Domain.Fleet.Drivers.Entities;

namespace SmartTaxi.Application.Fleet.Drivers.Abstractions;

public interface IDriverProfileRepository
{
    Task AddAsync(DriverProfile profile, CancellationToken cancellationToken);

    Task<DriverProfile?> GetByIdAsync(Guid driverProfileId, CancellationToken cancellationToken);

    Task<DriverProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverProfile>> GetEligibleAsync(CancellationToken cancellationToken);

    Task UpdateAsync(DriverProfile profile, CancellationToken cancellationToken);

    Task<bool> TrySubmitForReviewAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken);
}
