using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Notifications.Entities;

/// <summary>
/// A push-provider-issued device token for one user. Stored as-is (like Email),
/// not hashed like a refresh token: a push token is an opaque, provider-scoped
/// delivery address a real sender must present back to Firebase/APNs to send —
/// one-way hashing it would make sending impossible. It is still never returned
/// through any read API (see DeviceTokenSummary), and re-registering a token
/// already owned by another user reassigns it (phones get reset/resold) rather
/// than erroring.
/// </summary>
public sealed class DeviceToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastUsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive => RevokedAtUtc is null;

    private DeviceToken()
    {
    }

    private DeviceToken(Guid userId, string token, string platform, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        Token = token;
        Platform = platform;
        CreatedAtUtc = utcNow;
        LastUsedAtUtc = utcNow;
    }

    public static DeviceToken Register(Guid userId, string token, string platform, DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("L'utilisateur est requis.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Le jeton d'appareil est requis.");
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("La plateforme est requise.");
        }

        return new DeviceToken(userId, token.Trim(), platform.Trim(), utcNow);
    }

    public void Touch(DateTime utcNow) => LastUsedAtUtc = utcNow;

    /// <summary>Reassigns an existing row to its (possibly new) owner rather than erroring — see the type-level doc comment.</summary>
    public void Reassign(Guid newOwnerUserId, DateTime utcNow)
    {
        UserId = newOwnerUserId;
        RevokedAtUtc = null;
        LastUsedAtUtc = utcNow;
    }

    public void Revoke(DateTime utcNow) => RevokedAtUtc = utcNow;
}
