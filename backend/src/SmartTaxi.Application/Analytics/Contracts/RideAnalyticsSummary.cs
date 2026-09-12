namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>
/// Every metric is scoped by Ride.RequestedAt (the only reliable date field —
/// Ride has no dedicated CompletedAt/CancelledAt). "Completed"/"Cancelled"
/// therefore mean "requested in this period AND currently in that terminal
/// status," not "transitioned to that status in this period."
/// </summary>
public sealed record RideAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    int RideVolume,
    int Completed,
    int Cancelled,
    decimal CompletionRate,
    decimal AverageFare,
    decimal AverageDistanceKm,
    decimal AverageDurationMinutes);
