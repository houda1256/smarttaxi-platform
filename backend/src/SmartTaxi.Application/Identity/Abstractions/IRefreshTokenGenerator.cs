namespace SmartTaxi.Application.Identity.Abstractions;

public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new cryptographically random raw refresh token. The returned
    /// value is shown to the caller exactly once and must never be persisted.
    /// </summary>
    string Generate();
}
