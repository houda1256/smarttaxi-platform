using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverProfileRepository : IDriverProfileRepository
{
    private readonly Dictionary<Guid, DriverProfile> _profilesById = new();

    public Task AddAsync(DriverProfile profile, CancellationToken cancellationToken)
    {
        _profilesById[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task<DriverProfile?> GetByIdAsync(Guid driverProfileId, CancellationToken cancellationToken) =>
        Task.FromResult(_profilesById.GetValueOrDefault(driverProfileId));

    public Task<DriverProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_profilesById.Values.FirstOrDefault(p => p.UserId == userId));

    public Task<IReadOnlyCollection<DriverProfile>> GetEligibleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverProfile> profiles = _profilesById.Values
            .Where(p => p.VerificationStatus == DriverVerificationStatus.Approved && p.AvailabilityStatus == DriverAvailabilityStatus.Available)
            .ToList();
        return Task.FromResult(profiles);
    }

    public Task UpdateAsync(DriverProfile profile, CancellationToken cancellationToken)
    {
        _profilesById[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task<bool> TrySubmitForReviewAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_profilesById.TryGetValue(driverProfileId, out var profile) || profile.VerificationStatus != DriverVerificationStatus.PendingReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(profile, nameof(DriverProfile.VerificationStatus), DriverVerificationStatus.UnderReview);
        SetProperty(profile, nameof(DriverProfile.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryApproveAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_profilesById.TryGetValue(driverProfileId, out var profile) || profile.VerificationStatus != DriverVerificationStatus.UnderReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(profile, nameof(DriverProfile.VerificationStatus), DriverVerificationStatus.Approved);
        SetProperty(profile, nameof(DriverProfile.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_profilesById.TryGetValue(driverProfileId, out var profile) || profile.VerificationStatus != DriverVerificationStatus.UnderReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(profile, nameof(DriverProfile.VerificationStatus), DriverVerificationStatus.Rejected);
        SetProperty(profile, nameof(DriverProfile.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(Guid driverProfileId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_profilesById.TryGetValue(driverProfileId, out var profile) || profile.VerificationStatus != DriverVerificationStatus.Approved)
        {
            return Task.FromResult(false);
        }

        SetProperty(profile, nameof(DriverProfile.VerificationStatus), DriverVerificationStatus.Suspended);
        SetProperty(profile, nameof(DriverProfile.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(DriverProfile profile, string propertyName, object? value) =>
        typeof(DriverProfile).GetProperty(propertyName)!.SetValue(profile, value);
}
