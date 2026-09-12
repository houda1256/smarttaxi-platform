using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>Growth/active-count only — per-campaign impressions/clicks/CTR remain Advertising's own concern (GetCampaignPerformanceAdmin), never duplicated here.</summary>
internal sealed class AdvertisingAnalyticsReader : IAdvertisingAnalyticsReader
{
    private readonly ApplicationDbContext _context;

    public AdvertisingAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdvertisingAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var activeCampaigns = await _context.AdCampaigns.CountAsync(c => c.Status == AdCampaignStatus.Active, cancellationToken);

        var campaignGrowthCount = await _context.AdCampaigns
            .CountAsync(c => c.CreatedAtUtc >= fromUtc && c.CreatedAtUtc < toUtc, cancellationToken);

        return new AdvertisingAnalyticsSummary(fromUtc, toUtc, activeCampaigns, campaignGrowthCount);
    }
}
