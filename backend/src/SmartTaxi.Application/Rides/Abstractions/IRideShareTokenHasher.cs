namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideShareTokenHasher
{
    /// <summary>
    /// Deterministic, fast hash used only for the high-entropy random share
    /// token — mirrors IRefreshTokenHasher's reasoning (the token's own
    /// randomness, not hashing cost, is what makes it unguessable).
    /// </summary>
    string Hash(string rawToken);
}
