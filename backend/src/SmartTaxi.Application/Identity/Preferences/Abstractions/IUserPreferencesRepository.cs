using SmartTaxi.Domain.Identity.Preferences.Entities;

namespace SmartTaxi.Application.Identity.Preferences.Abstractions;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken);

    Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken);
}
