using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly Dictionary<Guid, UserPreferences> _preferencesByUserId = new();

    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_preferencesByUserId.GetValueOrDefault(userId));

    public Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        _preferencesByUserId[preferences.UserId] = preferences;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        _preferencesByUserId[preferences.UserId] = preferences;
        return Task.CompletedTask;
    }
}
