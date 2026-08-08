using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeReferralActivationPolicy : IReferralActivationPolicy
{
    public bool RequireEmailVerified { get; init; } = true;

    public bool RequirePhoneVerified { get; init; }

    public int MinAccountAgeDays { get; init; }
}
