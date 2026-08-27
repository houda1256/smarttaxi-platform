using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// PartnerGrowthCount counts only GaragePartner/RoadsideAssistancePartner
/// approvals (approved decision — the two roles literally named "...Partner"
/// in the domain vocabulary), using ReviewedAt (the moment they actually
/// became a partner), never CreatedAt (submission date). TaxiOwner (Fleet's
/// own concern) and Advertiser (has its own CampaignGrowth) are deliberately
/// excluded.
/// </summary>
internal sealed class FleetAnalyticsReader : IFleetAnalyticsReader
{
    private static readonly VehicleOperationalStatus[] ActiveVehicleStatuses =
    [
        VehicleOperationalStatus.Active, VehicleOperationalStatus.Assigned, VehicleOperationalStatus.InService
    ];

    private static readonly UserRole[] PartnerRoles = [UserRole.GaragePartner, UserRole.RoadsideAssistancePartner];

    private readonly ApplicationDbContext _context;

    public FleetAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FleetAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var activeVehicles = await _context.Vehicles.CountAsync(v => ActiveVehicleStatuses.Contains(v.OperationalStatus), cancellationToken);
        var vehicleGrowthCount = await _context.Vehicles.CountAsync(v => v.CreatedAt >= fromUtc && v.CreatedAt < toUtc, cancellationToken);

        var activeDrivers = await _context.DriverVehicleAssignments
            .Where(a => a.Status == AssignmentStatus.Active)
            .Select(a => a.DriverId)
            .Distinct()
            .CountAsync(cancellationToken);

        var partnerGrowthCount = await _context.ProfessionalAccountRequests
            .CountAsync(
                r => PartnerRoles.Contains(r.Role) && r.Status == ProfessionalAccountStatus.Approved && r.ReviewedAt >= fromUtc
                    && r.ReviewedAt < toUtc,
                cancellationToken);

        return new FleetAnalyticsSummary(fromUtc, toUtc, activeVehicles, vehicleGrowthCount, activeDrivers, partnerGrowthCount);
    }
}
