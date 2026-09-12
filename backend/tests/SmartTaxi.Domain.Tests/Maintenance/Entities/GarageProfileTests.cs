using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Events;

namespace SmartTaxi.Domain.Tests.Maintenance.Entities;

public class GarageProfileTests
{
    private static GarageProfile CreateValid() =>
        GarageProfile.Register(Guid.NewGuid(), "Garage Central", "Garage Central SARL", "12 rue X", "Tunis", "Standard,Comfort", "Vidange,Freins", DateTime.UtcNow);

    [Fact]
    public void Register_WithValidFields_RaisesGarageProfileRegistered()
    {
        var profile = CreateValid();

        var raised = Assert.Single(profile.DomainEvents);
        Assert.IsType<GarageProfileRegistered>(raised);
        Assert.True(profile.IsActive);
        Assert.Equal("TUNIS", profile.City);
    }

    [Theory]
    [InlineData("", "12 rue X", "Tunis")]
    [InlineData("Garage Central", "", "Tunis")]
    [InlineData("Garage Central", "12 rue X", "")]
    public void Register_WithMissingRequiredField_Throws(string businessName, string address, string city)
    {
        Assert.Throws<ArgumentException>(() =>
            GarageProfile.Register(Guid.NewGuid(), businessName, "Legal", address, city, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateProfile_WithValidFields_UpdatesAndTouchesUpdatedAtUtc()
    {
        var profile = CreateValid();
        var before = profile.UpdatedAtUtc;

        profile.UpdateProfile("New Name", "New Legal", "new addr", "Sousse", "Van", "Diagnostic", DateTime.UtcNow.AddMinutes(1));

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
