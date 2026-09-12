using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>
/// In-memory approximation of AdDeliveryTokenService's binding + expiration
/// semantics, without real cryptography — tokens are opaque GUIDs mapped to
/// their issuance context so Application-layer tests can construct forged
/// (unknown-string), expired, and cross-campaign/placement/user tokens
/// without depending on Infrastructure's Data Protection stack.
/// </summary>
public sealed class FakeAdDeliveryTokenService : IAdDeliveryTokenService
{
    private sealed record IssuedToken(Guid DeliveryId, Guid CampaignId, Guid PlacementId, Guid IssuedForUserId, DateTime ExpiresAtUtc);

    private readonly Dictionary<string, IssuedToken> _tokens = new();

    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(2);

    public string IssueToken(Guid campaignId, Guid placementId, Guid issuedForUserId, DateTime utcNow)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new IssuedToken(Guid.NewGuid(), campaignId, placementId, issuedForUserId, utcNow + TokenLifetime);
        return token;
    }

    /// <summary>Test helper: force a previously issued token past its expiration without waiting.</summary>
    public void ExpireToken(string token)
    {
        if (_tokens.TryGetValue(token, out var issued))
        {
            _tokens[token] = issued with { ExpiresAtUtc = DateTime.MinValue };
        }
    }

    public AdDeliveryTokenValidationResult Validate(string token, Guid campaignId, Guid placementId, Guid requestingUserId, DateTime utcNow)
    {
        if (!_tokens.TryGetValue(token, out var issued))
        {
            return AdDeliveryTokenValidationResult.Invalid("Jeton de diffusion invalide ou expiré.");
        }

        if (utcNow >= issued.ExpiresAtUtc)
        {
            return AdDeliveryTokenValidationResult.Invalid("Jeton de diffusion invalide ou expiré.");
        }

        if (issued.CampaignId != campaignId || issued.PlacementId != placementId || issued.IssuedForUserId != requestingUserId)
        {
            return AdDeliveryTokenValidationResult.Invalid("Ce jeton ne correspond pas à cette campagne, cet emplacement ou cet utilisateur.");
        }

        return AdDeliveryTokenValidationResult.Valid(issued.DeliveryId);
    }
}
