using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRoadsidePartnerProfileRepository : IRoadsidePartnerProfileRepository
{
    private readonly Dictionary<Guid, RoadsidePartnerProfile> _profiles = new();

    public Task<RoadsidePartnerProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.GetValueOrDefault(profileId));

    public Task<RoadsidePartnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.Values.FirstOrDefault(p => p.UserId == userId));

    public Task<bool> TryAddAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken)
    {
        if (_profiles.Values.Any(p => p.UserId == profile.UserId))
        {
            return Task.FromResult(false);
        }

        _profiles[profile.Id] = profile;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RoadsidePartnerProfile>> GetCompatibleCandidatesAsync(
        RoadsideServiceType serviceType, VehicleCategory vehicleCategory, string? city, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RoadsidePartnerProfile> candidates = _profiles.Values
            .Where(p => p.IsActive)
            .Where(p => p.SupportedServiceTypes is null || p.SupportedServiceTypes.Contains(serviceType.ToString()))
            .Where(p => p.SupportedVehicleCategories is null || p.SupportedVehicleCategories.Contains(vehicleCategory.ToString()))
            .Where(p => city is null || p.City == city)
            .ToList();

        return Task.FromResult(candidates);
    }
}
