namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>No GPS, no ETA, no route analytics — Latitude/Longitude are never selected by the underlying reader.</summary>
public sealed record RoadsideAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    int RequestCount,
    int CompletedCount,
    decimal CompletionRate,
    decimal AverageFinalCost,
    decimal AverageInterventionDurationMinutes);
