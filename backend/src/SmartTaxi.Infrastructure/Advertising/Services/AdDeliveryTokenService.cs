using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;

namespace SmartTaxi.Infrastructure.Advertising.Services;

/// <summary>
/// Uses ASP.NET Core's Data Protection API (already registered host-wide via
/// builder.Services.AddDataProtection() in Program.cs for
/// ITwoFactorSecretProtector — reused as-is, nothing new registered) via its
/// time-limited variant, which gives authenticated encryption (tamper-proof,
/// confidentiality) AND expiration natively — no hand-rolled HMAC/JWT code
/// needed. The payload is a simple pipe-delimited string; Unprotect throws
/// CryptographicException for both a tampered/forged payload and an expired
/// one, which is exactly the fail-closed behavior Validate needs.
/// </summary>
internal sealed class AdDeliveryTokenService : IAdDeliveryTokenService
{
    private const string Purpose = "SmartTaxi.Advertising.AdDeliveryToken.v1";
    private const string InvalidOrExpiredError = "Jeton de diffusion invalide ou expiré.";
    private const string MismatchError = "Ce jeton ne correspond pas à cette campagne, cet emplacement ou cet utilisateur.";

    private readonly ITimeLimitedDataProtector _protector;
    private readonly IAdvertisingDeliveryTokenPolicy _policy;

    public AdDeliveryTokenService(IDataProtectionProvider dataProtectionProvider, IAdvertisingDeliveryTokenPolicy policy)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
        _policy = policy;
    }

    public string IssueToken(Guid campaignId, Guid placementId, Guid issuedForUserId, DateTime utcNow)
    {
        var deliveryId = Guid.NewGuid();
        var payload = string.Join('|', deliveryId.ToString("N"), campaignId.ToString("N"), placementId.ToString("N"), issuedForUserId.ToString("N"));
        return _protector.Protect(payload, _policy.TokenLifetime);
    }

    public AdDeliveryTokenValidationResult Validate(string token, Guid campaignId, Guid placementId, Guid requestingUserId, DateTime utcNow)
    {
        string payload;

        try
        {
            payload = _protector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            // Covers both a tampered/forged token and a genuinely expired one — Data Protection's
            // time-limited protector does not distinguish the two, and callers don't need it to.
            return AdDeliveryTokenValidationResult.Invalid(InvalidOrExpiredError);
        }

        var parts = payload.Split('|');

        if (parts.Length != 4
            || !Guid.TryParse(parts[0], out var deliveryId)
            || !Guid.TryParse(parts[1], out var tokenCampaignId)
            || !Guid.TryParse(parts[2], out var tokenPlacementId)
            || !Guid.TryParse(parts[3], out var tokenUserId))
        {
            return AdDeliveryTokenValidationResult.Invalid(InvalidOrExpiredError);
        }

        if (tokenCampaignId != campaignId || tokenPlacementId != placementId || tokenUserId != requestingUserId)
        {
            return AdDeliveryTokenValidationResult.Invalid(MismatchError);
        }

        return AdDeliveryTokenValidationResult.Valid(deliveryId);
    }
}
