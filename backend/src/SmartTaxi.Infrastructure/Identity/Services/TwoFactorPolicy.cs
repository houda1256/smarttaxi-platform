using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class TwoFactorPolicy : ITwoFactorPolicy
{
    public int RecoveryCodeCount { get; }

    public TimeSpan ChallengeTokenLifetime { get; }

    public string Issuer { get; }

    public TwoFactorPolicy(IOptions<TwoFactorOptions> options)
    {
        var value = options.Value;
        RecoveryCodeCount = value.RecoveryCodeCount;
        ChallengeTokenLifetime = TimeSpan.FromMinutes(value.ChallengeTokenLifetimeMinutes);
        Issuer = value.Issuer;
    }
}
