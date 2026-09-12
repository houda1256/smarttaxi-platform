using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>
/// ActiveSubscriptions inlines Subscription.IsEffectivelyActive's own rule
/// (Status==Active AND EndDate&gt;utcNow) rather than calling the entity method
/// (not translatable to SQL) — kept in exact sync with that method's logic.
/// </summary>
internal sealed class SubscriptionAnalyticsReader : ISubscriptionAnalyticsReader
{
    private readonly ApplicationDbContext _context;

    public SubscriptionAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var activeSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && s.EndDate > utcNow, cancellationToken);

        var subscriptionGrowthCount = await _context.Subscriptions
            .CountAsync(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc, cancellationToken);

        return new SubscriptionAnalyticsSummary(fromUtc, toUtc, activeSubscriptions, subscriptionGrowthCount);
    }
}
