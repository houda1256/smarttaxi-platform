namespace SmartTaxi.Application.Identity.Abstractions;

/// <summary>
/// Configured lifetimes for refresh tokens and sessions. Application depends
/// only on this abstraction — the actual configuration binding lives in
/// Infrastructure, keeping Application free of any config-framework dependency.
/// </summary>
public interface IRefreshTokenPolicy
{
    TimeSpan TokenLifetime { get; }

    TimeSpan SessionLifetime { get; }
}
