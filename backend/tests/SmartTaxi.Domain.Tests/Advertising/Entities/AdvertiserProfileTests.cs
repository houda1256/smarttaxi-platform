using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Events;

namespace SmartTaxi.Domain.Tests.Advertising.Entities;

public class AdvertiserProfileTests
{
    private static AdvertiserProfile CreateValid() =>
        AdvertiserProfile.Register(Guid.NewGuid(), "Acme Ads", "Acme SARL", "TAX123", "Tunis", "12 rue X", "contact@acme.tn", "+21612345678", DateTime.UtcNow);

    [Fact]
    public void Register_WithValidFields_RaisesAdvertiserProfileRegistered()
    {
        var profile = CreateValid();

        var raised = Assert.Single(profile.DomainEvents);
        Assert.IsType<AdvertiserProfileRegistered>(raised);
        Assert.True(profile.IsActive);
    }

    [Theory]
    [InlineData("", "Legal", "TAX", "Tunis", "a@b.com")]
    [InlineData("Business", "", "TAX", "Tunis", "a@b.com")]
    [InlineData("Business", "Legal", "", "Tunis", "a@b.com")]
    [InlineData("Business", "Legal", "TAX", "", "a@b.com")]
    [InlineData("Business", "Legal", "TAX", "Tunis", "")]
    public void Register_WithMissingRequiredField_Throws(string businessName, string legalName, string taxIdentifier, string city, string email)
    {
        Assert.Throws<ArgumentException>(() =>
            AdvertiserProfile.Register(Guid.NewGuid(), businessName, legalName, taxIdentifier, city, "addr", email, null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateProfile_WithValidFields_UpdatesAndTouchesUpdatedAtUtc()
    {
        var profile = CreateValid();
        var before = profile.UpdatedAtUtc;

        profile.UpdateProfile("New Name", "New Legal", "TAX999", "Sousse", "new addr", "new@acme.tn", null, DateTime.UtcNow.AddMinutes(1));

        Assert.Equal("New Name", profile.BusinessName);
        Assert.Equal("Sousse", profile.City);
        Assert.True(profile.UpdatedAtUtc > before);
    }
}
