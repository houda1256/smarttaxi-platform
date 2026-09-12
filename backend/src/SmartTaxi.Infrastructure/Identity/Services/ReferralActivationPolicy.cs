using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class ReferralActivationPolicy : IReferralActivationPolicy
{
    private readonly ReferralActivationOptions _options;

    public ReferralActivationPolicy(IOptions<ReferralActivationOptions> options)
    {
        _options = options.Value;
    }

    public bool RequireEmailVerified => _options.RequireEmailVerified;

    public bool RequirePhoneVerified => _options.RequirePhoneVerified;

    public int MinAccountAgeDays => _options.MinAccountAgeDays;
}
