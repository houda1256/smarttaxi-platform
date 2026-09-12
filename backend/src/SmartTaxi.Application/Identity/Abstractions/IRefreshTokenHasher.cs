namespace SmartTaxi.Application.Identity.Abstractions;

public interface IRefreshTokenHasher
{
    /// <summary>
    /// Deterministic, fast hash used only for the high-entropy random refresh
    /// token — deliberately not the slow adaptive password hasher, since the
    /// token's own randomness (not hashing cost) is what makes it unguessable.
    /// </summary>
    string Hash(string rawToken);
}
