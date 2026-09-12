using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

internal sealed class RoadsidePartnerProfileRepository : IRoadsidePartnerProfileRepository
{
    private readonly ApplicationDbContext _context;

    public RoadsidePartnerProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RoadsidePartnerProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken) =>
        _context.RoadsidePartnerProfiles.FirstOrDefaultAsync(profile => profile.Id == profileId, cancellationToken);

    public Task<RoadsidePartnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.RoadsidePartnerProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task<bool> TryAddAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken)
    {
        await _context.RoadsidePartnerProfiles.AddAsync(profile, cancellationToken);

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

    public async Task UpdateAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken)
    {
        _context.RoadsidePartnerProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoadsidePartnerProfile>> GetCompatibleCandidatesAsync(
        RoadsideServiceType serviceType, VehicleCategory vehicleCategory, string? city, CancellationToken cancellationToken)
    {
        var serviceTypeText = serviceType.ToString();
        var vehicleCategoryText = vehicleCategory.ToString();

        var query = _context.RoadsidePartnerProfiles
            .Where(profile => profile.IsActive)
            .Where(profile => profile.SupportedServiceTypes == null || profile.SupportedServiceTypes.Contains(serviceTypeText))
            .Where(profile => profile.SupportedVehicleCategories == null || profile.SupportedVehicleCategories.Contains(vehicleCategoryText));

        if (city is not null)
        {
            query = query.Where(profile => profile.City == city);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
