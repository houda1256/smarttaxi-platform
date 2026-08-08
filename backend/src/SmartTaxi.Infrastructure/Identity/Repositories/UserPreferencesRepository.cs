using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ApplicationDbContext _context;

    public UserPreferencesRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.UserPreferences.FirstOrDefaultAsync(preferences => preferences.UserId == userId, cancellationToken);

    public async Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        await _context.UserPreferences.AddAsync(preferences, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
