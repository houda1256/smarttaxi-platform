using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdvertisingPlacementRepository
{
    Task<AdvertisingPlacement?> GetByIdAsync(Guid placementId, CancellationToken cancellationToken);

    Task<AdvertisingPlacement?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdvertisingPlacement>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdvertisingPlacement>> GetActiveAsync(CancellationToken cancellationToken);

    Task AddAsync(AdvertisingPlacement placement, CancellationToken cancellationToken);

    Task UpdateAsync(AdvertisingPlacement placement, CancellationToken cancellationToken);
}
