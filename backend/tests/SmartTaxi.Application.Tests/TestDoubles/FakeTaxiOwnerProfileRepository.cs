using SmartTaxi.Application.Fleet.Owners.Abstractions;
using SmartTaxi.Domain.Fleet.Owners.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTaxiOwnerProfileRepository : ITaxiOwnerProfileRepository
{
    private readonly Dictionary<Guid, TaxiOwnerProfile> _profilesById = new();

    public Task AddAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken)
    {
        _profilesById[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task<TaxiOwnerProfile?> GetByIdAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(_profilesById.GetValueOrDefault(ownerId));

    public Task<TaxiOwnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profilesById.Values.FirstOrDefault(p => p.UserId == userId));

    public Task UpdateAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken)
    {
        _profilesById[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public int Count => _profilesById.Count;
}
