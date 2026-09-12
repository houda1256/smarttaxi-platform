using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// Orchestrates the other 6 bucket readers (each already knows how to count
/// its own metric for one period) by calling each twice — current period,
/// then the equal-length previous period — rather than duplicating any of
/// their query logic. Only UserGrowth is computed directly here, since no
/// dedicated "User" bucket reader exists (Identity/User is not one of the 9
/// spec analytics buckets). Null CreatedAtUtc rows (users created before the
/// Module 12 migration) are automatically excluded by the SQL range
/// comparison itself — see User.CreatedAtUtc's own remarks for why no
/// historical value is fabricated for them.
/// </summary>
internal sealed class GrowthAnalyticsReader : IGrowthAnalyticsReader
{
    private const string UserGrowthNote =
        "UserGrowth n'est fiable que pour les utilisateurs créés après le déploiement de la migration CreatedAtUtc ; " +
        "les utilisateurs antérieurs n'ont aucune date d'inscription historiquement exacte et sont exclus du calcul.";

    private readonly ApplicationDbContext _context;
    private readonly IRideAnalyticsReader _rideReader;
    private readonly IFinancialAnalyticsReader _financialReader;
    private readonly ISubscriptionAnalyticsReader _subscriptionReader;
    private readonly IFleetAnalyticsReader _fleetReader;
    private readonly IAdvertisingAnalyticsReader _advertisingReader;
    private readonly ISupportAnalyticsReader _supportReader;

    public GrowthAnalyticsReader(
        ApplicationDbContext context, IRideAnalyticsReader rideReader, IFinancialAnalyticsReader financialReader,
        ISubscriptionAnalyticsReader subscriptionReader, IFleetAnalyticsReader fleetReader,
        IAdvertisingAnalyticsReader advertisingReader, ISupportAnalyticsReader supportReader)
    {
        _context = context;
        _rideReader = rideReader;
        _financialReader = financialReader;
        _subscriptionReader = subscriptionReader;
        _fleetReader = fleetReader;
        _advertisingReader = advertisingReader;
        _supportReader = supportReader;
    }

    public async Task<GrowthAnalyticsResult> GetGrowthAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var previousFromUtc = fromUtc - (toUtc - fromUtc);

        var userGrowth = await GetUserGrowthAsync(fromUtc, toUtc, previousFromUtc, cancellationToken);

        var currentRide = await _rideReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var previousRide = await _rideReader.GetSummaryAsync(previousFromUtc, fromUtc, cancellationToken);
        var rideGrowth = GrowthMetric.Compute(currentRide.RideVolume, previousRide.RideVolume);

        var financial = await _financialReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var revenueGrowth = financial.RevenueGrowth;

        var currentSubscription = await _subscriptionReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var previousSubscription = await _subscriptionReader.GetSummaryAsync(previousFromUtc, fromUtc, cancellationToken);
        var subscriptionGrowth = GrowthMetric.Compute(
            currentSubscription.SubscriptionGrowthCount, previousSubscription.SubscriptionGrowthCount);

        var currentFleet = await _fleetReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var previousFleet = await _fleetReader.GetSummaryAsync(previousFromUtc, fromUtc, cancellationToken);
        var partnerGrowth = GrowthMetric.Compute(currentFleet.PartnerGrowthCount, previousFleet.PartnerGrowthCount);
        var vehicleGrowth = GrowthMetric.Compute(currentFleet.VehicleGrowthCount, previousFleet.VehicleGrowthCount);

        var currentAdvertising = await _advertisingReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var previousAdvertising = await _advertisingReader.GetSummaryAsync(previousFromUtc, fromUtc, cancellationToken);
        var campaignGrowth = GrowthMetric.Compute(currentAdvertising.CampaignGrowthCount, previousAdvertising.CampaignGrowthCount);

        var currentSupport = await _supportReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
        var previousSupport = await _supportReader.GetSummaryAsync(previousFromUtc, fromUtc, cancellationToken);
        var supportTicketGrowth = GrowthMetric.Compute(
            currentSupport.SupportTicketGrowthCount, previousSupport.SupportTicketGrowthCount);
        var incidentGrowth = GrowthMetric.Compute(currentSupport.IncidentGrowthCount, previousSupport.IncidentGrowthCount);

        return new GrowthAnalyticsResult(
            fromUtc, toUtc, userGrowth, rideGrowth, revenueGrowth, subscriptionGrowth, partnerGrowth, vehicleGrowth, campaignGrowth,
            supportTicketGrowth, incidentGrowth, UserGrowthNote);
    }

    private async Task<GrowthMetric> GetUserGrowthAsync(
        DateTime fromUtc, DateTime toUtc, DateTime previousFromUtc, CancellationToken cancellationToken)
    {
        var currentCount = await _context.Users.CountAsync(u => u.CreatedAtUtc >= fromUtc && u.CreatedAtUtc < toUtc, cancellationToken);
        var previousCount = await _context.Users
            .CountAsync(u => u.CreatedAtUtc >= previousFromUtc && u.CreatedAtUtc < fromUtc, cancellationToken);

        return GrowthMetric.Compute(currentCount, previousCount);
    }
}
