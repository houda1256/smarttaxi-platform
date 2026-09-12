using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Events;

namespace SmartTaxi.Domain.Tests.RoadsideAssistance.Entities;

public class RoadsidePartnerProfileTests
{
    private static RoadsidePartnerProfile CreateValid() =>
        RoadsidePartnerProfile.Register(
            Guid.NewGuid(), "Assistance Rapide", "Assistance Rapide SARL", "12 rue X", "Tunis", "Towing,BatteryJumpStart",
            "Standard,Comfort", 36.8, 10.18, DateTime.UtcNow);

    [Fact]
    public void Register_WithValidFields_RaisesRoadsidePartnerProfileRegistered()
    {
        var profile = CreateValid();

        var raised = Assert.Single(profile.DomainEvents);
        Assert.IsType<RoadsidePartnerProfileRegistered>(raised);
        Assert.True(profile.IsActive);
        Assert.Equal("TUNIS", profile.City);
        Assert.Equal(36.8, profile.Latitude);
    }

    [Theory]
    [InlineData("", "12 rue X", "Tunis")]
    [InlineData("Assistance Rapide", "", "Tunis")]
    [InlineData("Assistance Rapide", "12 rue X", "")]
    public void Register_WithMissingRequiredField_Throws(string businessName, string address, string city)
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsidePartnerProfile.Register(Guid.NewGuid(), businessName, "Legal", address, city, null, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Register_WithOnlyLatitudeProvided_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsidePartnerProfile.Register(Guid.NewGuid(), "Assistance Rapide", null, "12 rue X", "Tunis", null, null, 36.8, null, DateTime.UtcNow));
    }

    [Fact]
    public void Register_WithOutOfRangeLatitude_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsidePartnerProfile.Register(Guid.NewGuid(), "Assistance Rapide", null, "12 rue X", "Tunis", null, null, 200, 10.18, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateProfile_WithValidFields_UpdatesAndTouchesUpdatedAtUtc()
    {
        var profile = CreateValid();
        var before = profile.UpdatedAtUtc;

        profile.UpdateProfile("New Name", "New Legal", "new addr", "Sousse", "Towing", "Van", 35.8, 10.6, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal("New Name", profile.BusinessName);
        Assert.Equal("SOUSSE", profile.City);
        Assert.True(profile.UpdatedAtUtc > before);
    }

    [Fact]
    public void Deactivate_ThenReactivate_TogglesIsActive()
    {
        var profile = CreateValid();

        profile.Deactivate(DateTime.UtcNow);
        Assert.False(profile.IsActive);

        profile.Reactivate(DateTime.UtcNow);
        Assert.True(profile.IsActive);
    }
}
