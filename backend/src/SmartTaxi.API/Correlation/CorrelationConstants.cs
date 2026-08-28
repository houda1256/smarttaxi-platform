namespace SmartTaxi.API.Correlation;

/// <summary>
/// The one canonical application correlation identifier — never confused with
/// HttpContext.TraceIdentifier (left untouched) or Activity.Current.TraceId
/// (reserved for future OpenTelemetry wiring, not implemented here).
/// </summary>
public static class CorrelationConstants
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";

    private const int MaxLength = 64;

    /// <summary>Length 1..64, restricted to [A-Za-z0-9-] — anything else is never trusted or reflected back.</summary>
    public static bool IsValid(string value) =>
        value.Length is > 0 and <= MaxLength && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
}
