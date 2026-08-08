using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideShareTokenRepository : IRideShareTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RideShareTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideShareToken token, CancellationToken cancellationToken)
    {
        await _context.RideShareTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RideShareToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken) =>
        _context.RideShareTokens.FirstOrDefaultAsync(token => token.Id == tokenId, cancellationToken);

    public Task<RideShareToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.RideShareTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<RideShareToken?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.RideShareTokens
            .Where(token => token.RideId == rideId && token.RevokedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryRevokeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RideShareTokens
            .Where(token => token.Id == tokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
