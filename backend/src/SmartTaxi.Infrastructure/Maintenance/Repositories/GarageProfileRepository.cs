using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

internal sealed class GarageProfileRepository : IGarageProfileRepository
{
    private readonly ApplicationDbContext _context;

    public GarageProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<GarageProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        _context.GarageProfiles.FirstOrDefaultAsync(profile => profile.Id == profileId, cancellationToken);

    public Task<GarageProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.GarageProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task<bool> TryAddAsync(GarageProfile profile, CancellationToken cancellationToken)
    {
        await _context.GarageProfiles.AddAsync(profile, cancellationToken);

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

    public async Task UpdateAsync(GarageProfile profile, CancellationToken cancellationToken)
    {
        _context.GarageProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
