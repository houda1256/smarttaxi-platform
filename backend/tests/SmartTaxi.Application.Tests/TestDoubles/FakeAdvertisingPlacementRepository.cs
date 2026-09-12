using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdvertisingPlacementRepository : IAdvertisingPlacementRepository
{
    private readonly Dictionary<Guid, AdvertisingPlacement> _placements = new();

    public Task<AdvertisingPlacement?> GetByIdAsync(Guid placementId, CancellationToken cancellationToken) =>
        Task.FromResult(_placements.GetValueOrDefault(placementId));

    public Task<AdvertisingPlacement?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_placements.Values.FirstOrDefault(p => p.Code == code.Trim().ToUpper()));

    public Task<IReadOnlyCollection<AdvertisingPlacement>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdvertisingPlacement>>(_placements.Values.ToList());

    public Task<IReadOnlyCollection<AdvertisingPlacement>> GetActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdvertisingPlacement>>(_placements.Values.Where(p => p.IsActive).ToList());

    public Task AddAsync(AdvertisingPlacement placement, CancellationToken cancellationToken)
    {
        _placements[placement.Id] = placement;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AdvertisingPlacement placement, CancellationToken cancellationToken)
    {
        _placements[placement.Id] = placement;
        return Task.CompletedTask;
    }

    public void Seed(params AdvertisingPlacement[] placements)
    {
        foreach (var placement in placements)
        {
            _placements[placement.Id] = placement;
        }
    }
}
