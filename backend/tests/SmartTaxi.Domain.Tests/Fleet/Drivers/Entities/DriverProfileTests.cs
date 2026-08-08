using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Events;

namespace SmartTaxi.Domain.Tests.Fleet.Drivers.Entities;

public class DriverProfileTests
{
    private static DriverProfile CreateProfile() => DriverProfile.Create(
        Guid.NewGuid(), "LIC123", DateTime.UtcNow.AddYears(2), "TAXI123", independentDriver: true, DateTime.UtcNow);

    [Fact]
    public void Create_SetsPendingReviewAndOfflineDefaults()
    {
        var profile = CreateProfile();

        Assert.Equal(DriverVerificationStatus.PendingReview, profile.VerificationStatus);
        Assert.Equal(DriverAvailabilityStatus.Offline, profile.AvailabilityStatus);
        Assert.Equal(0, profile.CompletedRideCount);
        Assert.Single(profile.DomainEvents);
        Assert.IsType<DriverProfileCreated>(profile.DomainEvents.Single());
    }

    [Fact]
    public void SetAvailability_BeforeApproval_ToNonOffline_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<InvalidOperationException>(() => profile.SetAvailability(DriverAvailabilityStatus.Available, DateTime.UtcNow));
    }

    [Fact]
    public void SetAvailability_BeforeApproval_ToOffline_Succeeds()
    {
        var profile = CreateProfile();

        profile.SetAvailability(DriverAvailabilityStatus.Offline, DateTime.UtcNow);

        Assert.Equal(DriverAvailabilityStatus.Offline, profile.AvailabilityStatus);
    }

    [Fact]
    public void SetAvailability_AfterApproval_RaisesAvailabilityChangedEvent()
    {
        var profile = CreateProfile();
        typeof(DriverProfile).GetProperty(nameof(DriverProfile.VerificationStatus))!
            .SetValue(profile, DriverVerificationStatus.Approved);

        profile.SetAvailability(DriverAvailabilityStatus.Available, DateTime.UtcNow);

        Assert.Equal(DriverAvailabilityStatus.Available, profile.AvailabilityStatus);
        Assert.Contains(profile.DomainEvents, e => e is DriverAvailabilityChanged);
    }

    [Fact]
    public void AssignVehicle_ThenUnassign_ClearsCurrentVehicleId()
    {
        var profile = CreateProfile();
        var vehicleId = Guid.NewGuid();

        profile.AssignVehicle(vehicleId, DateTime.UtcNow);
        Assert.Equal(vehicleId, profile.CurrentVehicleId);

        profile.UnassignVehicle(DateTime.UtcNow);
        Assert.Null(profile.CurrentVehicleId);
    }
}
