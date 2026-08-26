using SmartTaxi.Application.Advertising.Contracts;

namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>
/// Server-issued, short-lived, cryptographically protected proof that a
/// specific authenticated user was legitimately granted a delivery context
/// for a specific (CampaignId, PlacementId) pair — see the Module 8 audit fix
/// #1. Replaces accepting a bare client-supplied IdempotencyKey for
/// impressions/clicks, which let any caller fabricate unlimited fraudulent
/// budget-consuming events. A token binds CampaignId + PlacementId + the
/// issuing user + issued-at/expiration + a unique delivery identifier, and
/// the client can never substitute a different campaign/placement/user
/// without invalidating it (Validate checks every bound field against what
/// the caller is trying to record against).
/// </summary>
public interface IAdDeliveryTokenService
{
    /// <summary>Server-side only — never callable with attacker-controlled inputs beyond what RequestAdDeliveryCommandHandler already validated (campaign exists, is Active, placement matches).</summary>
    string IssueToken(Guid campaignId, Guid placementId, Guid issuedForUserId, DateTime utcNow);

    /// <summary>Fails closed on any tamper, expiry, or mismatch against the campaign/placement/user the caller is actually trying to record an event for.</summary>
    AdDeliveryTokenValidationResult Validate(string token, Guid campaignId, Guid placementId, Guid requestingUserId, DateTime utcNow);
}
