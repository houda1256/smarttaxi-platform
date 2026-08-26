using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeGarageProfileRepository : IGarageProfileRepository
{
    private readonly Dictionary<Guid, GarageProfile> _profiles = new();

    public Task<GarageProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(profileId));

    public Task<GarageProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.Values.FirstOrDefault(p => p.UserId == userId));

    public Task<bool> TryAddAsync(GarageProfile profile, CancellationToken cancellationToken)
    {
        if (_profiles.Values.Any(p => p.UserId == profile.UserId))
        {
            return Task.FromResult(false);
        }

        _profiles[profile.Id] = profile;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(GarageProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Id] = profile;
        return Task.CompletedTask;
    }
}
