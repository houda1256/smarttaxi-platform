using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// A pure current-state snapshot — deliberately self-contained (no date
/// range, no dependency on the other bucket readers) like
/// FinancialReportRepository itself. ActiveCustomers reaches User's owned
/// "_roleAssignments" collection via EF.Property, the officially supported
/// way to query a field-backed navigation that has no public queryable
/// property (User.Roles is a computed in-memory projection, not translatable
/// to SQL).
/// </summary>
internal sealed class AdminDashboardReader : IAdminDashboardReader
{
    private static readonly RideStatus[] TerminalRideStatuses =
    [
        RideStatus.Completed, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver, RideStatus.CancelledByAdmin,
        RideStatus.Expired, RideStatus.NoDriverAvailable, RideStatus.CustomerNoShow, RideStatus.DriverNoShow, RideStatus.Disputed
    ];

    private static readonly RideStatus[] CancelledRideStatuses =
    [
        RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver, RideStatus.CancelledByAdmin
    ];

    private static readonly VehicleOperationalStatus[] ActiveVehicleStatuses =
    [
        VehicleOperationalStatus.Active, VehicleOperationalStatus.Assigned, VehicleOperationalStatus.InService
    ];

    private static readonly SupportTicketStatus[] OpenTicketStatuses =
    [
        SupportTicketStatus.Open, SupportTicketStatus.Assigned, SupportTicketStatus.InProgress,
        SupportTicketStatus.WaitingForCustomer, SupportTicketStatus.Reopened
    ];

    private static readonly SupportIncidentStatus[] OpenIncidentStatuses =
    [
        SupportIncidentStatus.Reported, SupportIncidentStatus.Acknowledged, SupportIncidentStatus.Investigating,
        SupportIncidentStatus.Reopened
    ];

    private readonly ApplicationDbContext _context;

    public AdminDashboardReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardSummary> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var totalUsers = await _context.Users.CountAsync(cancellationToken);

        var activeCustomers = await _context.Users
            .Where(u => u.IsActive)
            .SelectMany(
                u => EF.Property<IEnumerable<UserRoleAssignment>>(u, "_roleAssignments"), (u, role) => new { u.Id, role.Role })
            .Where(x => x.Role == UserRole.Customer)
            .Select(x => x.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var activeDrivers = await _context.DriverVehicleAssignments
            .Where(a => a.Status == AssignmentStatus.Active)
            .Select(a => a.DriverId)
            .Distinct()
            .CountAsync(cancellationToken);

        var activeVehicles = await _context.Vehicles.CountAsync(v => ActiveVehicleStatuses.Contains(v.OperationalStatus), cancellationToken);

        var activeRides = await _context.Rides.CountAsync(r => !TerminalRideStatuses.Contains(r.Status), cancellationToken);
        var completedRides = await _context.Rides.CountAsync(r => r.Status == RideStatus.Completed, cancellationToken);
        var cancelledRides = await _context.Rides.CountAsync(r => CancelledRideStatuses.Contains(r.Status), cancellationToken);

        var totalRevenue = await _context.Payments
            .Where(p => p.ConfirmedAt != null)
            .SumAsync(p => (decimal?)p.FinalFareAmount, cancellationToken) ?? 0m;

        var platformCommission = await _context.FinancialLedgerEntries
            .Where(e => e.EntryType == LedgerEntryType.PlatformCommission)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var pendingPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Pending, cancellationToken);

        var activeSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && s.EndDate > utcNow, cancellationToken);

        var openSupportTickets = await _context.SupportTickets
            .CountAsync(t => OpenTicketStatuses.Contains(t.Status), cancellationToken);

        var criticalIncidents = await _context.SupportIncidents
            .CountAsync(i => i.Severity == SupportIncidentSeverity.Critical && OpenIncidentStatuses.Contains(i.Status), cancellationToken);

        var pendingPartnerApprovals = await _context.ProfessionalAccountRequests
            .CountAsync(r => r.Status == ProfessionalAccountStatus.PendingReview, cancellationToken);

        var activeCampaigns = await _context.AdCampaigns.CountAsync(c => c.Status == AdCampaignStatus.Active, cancellationToken);

        return new AdminDashboardSummary(
            totalUsers, activeCustomers, activeDrivers, activeVehicles, activeRides, completedRides, cancelledRides,
            Math.Round(totalRevenue, 2), Math.Round(platformCommission, 2), pendingPayments, activeSubscriptions, openSupportTickets,
            criticalIncidents, pendingPartnerApprovals, activeCampaigns);
    }
}
