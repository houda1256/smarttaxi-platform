using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

internal sealed class AdvertiserProfileRepository : IAdvertiserProfileRepository
{
    private readonly ApplicationDbContext _context;

    public AdvertiserProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AdvertiserProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        _context.AdvertiserProfiles.FirstOrDefaultAsync(profile => profile.Id == profileId, cancellationToken);

    public Task<AdvertiserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.AdvertiserProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task<bool> TryAddAsync(AdvertiserProfile profile, CancellationToken cancellationToken)
    {
        await _context.AdvertiserProfiles.AddAsync(profile, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(AdvertiserProfile profile, CancellationToken cancellationToken)
    {
        _context.AdvertiserProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
