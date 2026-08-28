namespace SmartTaxi.Infrastructure.Configuration;

/// <summary>
/// Empty by default in every environment — no browser frontend origin is
/// authoritative yet (native/mobile clients and server-to-server callers
/// never need CORS at all). Never AllowAnyOrigin.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}
