namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>Counts/severities/durations only — never ticket message bodies, internal notes, or incident titles/descriptions.</summary>
public sealed record SupportAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    int OpenSupportTickets,
    int CriticalIncidents,
    int SupportTicketGrowthCount,
    int IncidentGrowthCount,
    decimal AverageResolutionHours);
