namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>
/// Current-state snapshot, not period-scoped — distinct from GrowthAnalyticsResult.
/// TotalRevenue/PlatformCommission are all-time figures reused from Payments'
/// own existing aggregation, never recomputed independently (see
/// IFinancialAnalyticsReader's own remarks). SystemHealth is deliberately
/// absent — Module 13B.
/// </summary>
public sealed record AdminDashboardSummary(
    int TotalUsers,
    int ActiveCustomers,
    int ActiveDrivers,
    int ActiveVehicles,
    int ActiveRides,
    int CompletedRides,
    int CancelledRides,
    decimal TotalRevenue,
    decimal PlatformCommission,
    int PendingPayments,
    int ActiveSubscriptions,
    int OpenSupportTickets,
    int CriticalIncidents,
    int PendingPartnerApprovals,
    int ActiveCampaigns);
