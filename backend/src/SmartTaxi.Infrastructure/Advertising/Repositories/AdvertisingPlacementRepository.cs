using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

internal sealed class AdvertisingPlacementRepository : IAdvertisingPlacementRepository
{
    private readonly ApplicationDbContext _context;

    public AdvertisingPlacementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AdvertisingPlacement?> GetByIdAsync(Guid placementId, CancellationToken cancellationToken) =>
        _context.AdvertisingPlacements.FirstOrDefaultAsync(placement => placement.Id == placementId, cancellationToken);

    public Task<AdvertisingPlacement?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.AdvertisingPlacements.FirstOrDefaultAsync(placement => placement.Code == code.Trim().ToUpper(), cancellationToken);

    public async Task<IReadOnlyCollection<AdvertisingPlacement>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.AdvertisingPlacements.OrderBy(placement => placement.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AdvertisingPlacement>> GetActiveAsync(CancellationToken cancellationToken) =>
        await _context.AdvertisingPlacements.Where(placement => placement.IsActive).OrderBy(placement => placement.Code).ToListAsync(cancellationToken);

    public async Task AddAsync(AdvertisingPlacement placement, CancellationToken cancellationToken)
    {
        await _context.AdvertisingPlacements.AddAsync(placement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdvertisingPlacement placement, CancellationToken cancellationToken)
    {
        _context.AdvertisingPlacements.Update(placement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
