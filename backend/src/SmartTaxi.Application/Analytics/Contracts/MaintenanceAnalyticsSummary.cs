namespace SmartTaxi.Application.Analytics.Contracts;

public sealed record MaintenanceAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    int RequestCount,
    int CompletedCount,
    decimal CompletionRate,
    decimal AverageFinalCost,
    decimal AverageTurnaroundHours);
