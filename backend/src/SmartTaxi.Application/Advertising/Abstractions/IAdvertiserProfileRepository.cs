using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdvertiserProfileRepository
{
    Task<AdvertiserProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken);

    Task<AdvertiserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Returns false (instead of throwing) if a concurrent registration already created a profile for this user — the unique index on UserId is the real enforcement point.</summary>
    Task<bool> TryAddAsync(AdvertiserProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(AdvertiserProfile profile, CancellationToken cancellationToken);
}
