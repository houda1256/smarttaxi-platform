using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Infrastructure.Notifications.Options;
using SmartTaxi.Infrastructure.Notifications.Policies;

namespace SmartTaxi.Infrastructure.Tests.Notifications.Policies;

public class NotificationPreferencePolicyTests
{
    private sealed class StubUserPreferencesRepository : IUserPreferencesRepository
    {
        private readonly UserPreferences? _preferences;

        public StubUserPreferencesRepository(UserPreferences? preferences) => _preferences = preferences;

        public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_preferences);

        public Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static NotificationOptions DefaultOptions => new() { MandatoryChannels = NotificationChannel.Email | NotificationChannel.InApp };

    [Fact]
    public async Task ResolveChannels_Mandatory_IgnoresUserPreferencesEntirely()
    {
        var preferences = UserPreferences.CreateDefault(Guid.NewGuid(), DateTime.UtcNow);
        preferences.Update(Language.French, NotificationChannel.None, false, false, "UTC", null, null, DateTime.UtcNow);
        var policy = new NotificationPreferencePolicy(new StubUserPreferencesRepository(preferences), Options.Create(DefaultOptions));

        var channels = await policy.ResolveChannelsAsync(Guid.NewGuid(), isMandatory: true, CancellationToken.None);

        Assert.Contains(NotificationChannel.Email, channels);
        Assert.Contains(NotificationChannel.InApp, channels);
    }

    [Fact]
    public async Task ResolveChannels_Optional_RespectsUserPreferences()
    {
        var preferences = UserPreferences.CreateDefault(Guid.NewGuid(), DateTime.UtcNow);
        preferences.Update(Language.French, NotificationChannel.Sms, false, false, "UTC", null, null, DateTime.UtcNow);
        var policy = new NotificationPreferencePolicy(new StubUserPreferencesRepository(preferences), Options.Create(DefaultOptions));

        var channels = await policy.ResolveChannelsAsync(Guid.NewGuid(), isMandatory: false, CancellationToken.None);

        Assert.Single(channels, NotificationChannel.Sms);
    }

    [Fact]
    public async Task ResolveChannels_Optional_WhenUserOptedOutOfEverything_ReturnsEmpty()
    {
        var preferences = UserPreferences.CreateDefault(Guid.NewGuid(), DateTime.UtcNow);
        preferences.Update(Language.French, NotificationChannel.None, false, false, "UTC", null, null, DateTime.UtcNow);
        var policy = new NotificationPreferencePolicy(new StubUserPreferencesRepository(preferences), Options.Create(DefaultOptions));

        var channels = await policy.ResolveChannelsAsync(Guid.NewGuid(), isMandatory: false, CancellationToken.None);

        Assert.Empty(channels);
    }

    [Fact]
    public async Task ResolveChannels_Optional_NoPreferencesRow_FallsBackToDefault()
    {
        var policy = new NotificationPreferencePolicy(new StubUserPreferencesRepository(null), Options.Create(DefaultOptions));

        var channels = await policy.ResolveChannelsAsync(Guid.NewGuid(), isMandatory: false, CancellationToken.None);

        Assert.Contains(NotificationChannel.Email, channels);
        Assert.Contains(NotificationChannel.InApp, channels);
    }
}
