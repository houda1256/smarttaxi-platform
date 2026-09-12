using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Owners.Abstractions;
using SmartTaxi.Domain.Fleet.Owners.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class TaxiOwnerProfileRepository : ITaxiOwnerProfileRepository
{
    private readonly ApplicationDbContext _context;

    public TaxiOwnerProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken)
    {
        await _context.TaxiOwnerProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<TaxiOwnerProfile?> GetByIdAsync(Guid ownerId, CancellationToken cancellationToken) =>
        _context.TaxiOwnerProfiles.FirstOrDefaultAsync(profile => profile.Id == ownerId, cancellationToken);

    public Task<TaxiOwnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.TaxiOwnerProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public Task UpdateAsync(TaxiOwnerProfile profile, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
