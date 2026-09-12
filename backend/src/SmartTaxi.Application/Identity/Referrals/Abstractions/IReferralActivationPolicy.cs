namespace SmartTaxi.Application.Identity.Referrals.Abstractions;

/// <summary>Configurable conditions a referee must satisfy before a referral becomes reward-eligible.</summary>
public interface IReferralActivationPolicy
{
    bool RequireEmailVerified { get; }

    bool RequirePhoneVerified { get; }

    int MinAccountAgeDays { get; }
}
