using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Readers;

/// <summary>Counts/severities/durations only — never ticket message bodies, internal notes, or incident titles/descriptions (never selected here at all).</summary>
internal sealed class SupportAnalyticsReader : ISupportAnalyticsReader
{
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

    public SupportAnalyticsReader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupportAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var openSupportTickets = await _context.SupportTickets
            .CountAsync(t => OpenTicketStatuses.Contains(t.Status), cancellationToken);

        var criticalIncidents = await _context.SupportIncidents
            .CountAsync(i => i.Severity == SupportIncidentSeverity.Critical && OpenIncidentStatuses.Contains(i.Status), cancellationToken);

        var supportTicketGrowthCount = await _context.SupportTickets
            .CountAsync(t => t.CreatedAtUtc >= fromUtc && t.CreatedAtUtc < toUtc, cancellationToken);

        var incidentGrowthCount = await _context.SupportIncidents
            .CountAsync(i => i.CreatedAtUtc >= fromUtc && i.CreatedAtUtc < toUtc, cancellationToken);

        var resolutionTimes = await _context.SupportTickets
            .Where(t => t.ResolvedAtUtc != null && t.CreatedAtUtc >= fromUtc && t.CreatedAtUtc < toUtc)
            .Select(t => new { t.CreatedAtUtc, ResolvedAtUtc = t.ResolvedAtUtc!.Value })
            .ToListAsync(cancellationToken);

        var averageResolutionHours = resolutionTimes.Count == 0
            ? 0m
            : (decimal)resolutionTimes.Average(t => (t.ResolvedAtUtc - t.CreatedAtUtc).TotalHours);

        return new SupportAnalyticsSummary(
            fromUtc, toUtc, openSupportTickets, criticalIncidents, supportTicketGrowthCount, incidentGrowthCount,
            Math.Round(averageResolutionHours, 2));
    }
}
