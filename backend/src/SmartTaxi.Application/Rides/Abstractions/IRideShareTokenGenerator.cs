namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideShareTokenGenerator
{
    /// <summary>
    /// Generates a new cryptographically random raw share token. The returned
    /// value is shown to the caller exactly once and must never be persisted.
    /// </summary>
    string Generate();
}
