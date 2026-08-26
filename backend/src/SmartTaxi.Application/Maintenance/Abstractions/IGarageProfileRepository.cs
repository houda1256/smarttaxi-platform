using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Abstractions;

public interface IGarageProfileRepository
{
    Task<GarageProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken);

    Task<GarageProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Returns false (instead of throwing) if a concurrent registration already created a profile for this user — the unique index on UserId is the real enforcement point.</summary>
    Task<bool> TryAddAsync(GarageProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(GarageProfile profile, CancellationToken cancellationToken);
}
