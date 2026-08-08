using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Preferences.Entities;

public class UserPreferencesTests
{
    [Fact]
    public void CreateDefault_SetsSensibleDefaults()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var preferences = UserPreferences.CreateDefault(userId, utcNow);

        Assert.Equal(userId, preferences.UserId);
        Assert.Equal(Language.French, preferences.Language);
        Assert.Equal(NotificationChannel.Email | NotificationChannel.InApp, preferences.NotificationChannels);
        Assert.False(preferences.ShareProfileWithPartners);
        Assert.False(preferences.AllowMarketingCommunications);
        Assert.Equal("UTC", preferences.Timezone);
    }

    [Fact]
    public void Update_ReplacesAllMutableFieldsAndBumpsUpdatedAt()
    {
        var preferences = UserPreferences.CreateDefault(Guid.NewGuid(), DateTime.UtcNow);
        var newUtcNow = DateTime.UtcNow.AddDays(1);

        preferences.Update(
            Language.Arabic,
            NotificationChannel.Sms | NotificationChannel.Push,
            shareProfileWithPartners: true,
            allowMarketingCommunications: true,
            "Africa/Casablanca",
            "Nouveau Nom",
            "https://example.com/avatar.png",
            newUtcNow);

        Assert.Equal(Language.Arabic, preferences.Language);
        Assert.Equal(NotificationChannel.Sms | NotificationChannel.Push, preferences.NotificationChannels);
        Assert.True(preferences.ShareProfileWithPartners);
        Assert.True(preferences.AllowMarketingCommunications);
        Assert.Equal("Africa/Casablanca", preferences.Timezone);
        Assert.Equal("Nouveau Nom", preferences.DisplayName);
        Assert.Equal(newUtcNow, preferences.UpdatedAt);
    }
}
