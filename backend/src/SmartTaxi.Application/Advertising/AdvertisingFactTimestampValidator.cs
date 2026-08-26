namespace SmartTaxi.Application.Advertising;

/// <summary>
/// A caller-supplied OccurredAtUtc is informational only (display/reporting
/// purposes) — it is never used for budget/security decisions (those always
/// use server DateTime.UtcNow). This only rejects values so far off from
/// server time that they cannot be genuine (Module 8 audit LOW finding),
/// tolerating ordinary clock skew.
/// </summary>
internal static class AdvertisingFactTimestampValidator
{
    private static readonly TimeSpan MaxFuture = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxPast = TimeSpan.FromHours(1);

    public static bool IsReasonable(DateTime occurredAtUtc, DateTime utcNow) =>
        occurredAtUtc <= utcNow + MaxFuture && occurredAtUtc >= utcNow - MaxPast;
}
