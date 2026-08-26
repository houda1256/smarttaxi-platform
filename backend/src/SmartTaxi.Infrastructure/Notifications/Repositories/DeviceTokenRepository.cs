using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Notifications.Repositories;

internal sealed class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly ApplicationDbContext _context;

    public DeviceTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        _context.DeviceTokens.FirstOrDefaultAsync(deviceToken => deviceToken.Token == token, cancellationToken);

    public Task<DeviceToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.DeviceTokens.FirstOrDefaultAsync(deviceToken => deviceToken.Id == id, cancellationToken);

    public async Task<bool> TryAddAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        await _context.DeviceTokens.AddAsync(token, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(token).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        _context.DeviceTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DeviceToken>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.DeviceTokens
            .Where(deviceToken => deviceToken.UserId == userId && deviceToken.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
}
