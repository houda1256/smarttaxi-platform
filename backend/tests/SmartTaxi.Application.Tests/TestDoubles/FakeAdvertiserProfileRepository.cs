using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdvertiserProfileRepository : IAdvertiserProfileRepository
{
    private readonly Dictionary<Guid, AdvertiserProfile> _profiles = new();

    public Task<AdvertiserProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(profileId));

    public Task<AdvertiserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.Values.FirstOrDefault(p => p.UserId == userId));

    public Task<bool> TryAddAsync(AdvertiserProfile profile, CancellationToken cancellationToken)
    {
        if (_profiles.Values.Any(p => p.UserId == profile.UserId))
        {
            return Task.FromResult(false);
        }

        _profiles[profile.Id] = profile;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(AdvertiserProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Id] = profile;
        return Task.CompletedTask;
    }
}
