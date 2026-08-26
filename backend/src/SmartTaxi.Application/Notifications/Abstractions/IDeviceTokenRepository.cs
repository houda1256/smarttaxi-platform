using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Abstractions;

public interface IDeviceTokenRepository
{
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    Task<DeviceToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>False when the partial unique index (active tokens only) rejects a concurrent duplicate registration of the same raw token.</summary>
    Task<bool> TryAddAsync(DeviceToken token, CancellationToken cancellationToken);

    Task UpdateAsync(DeviceToken token, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DeviceToken>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
}
