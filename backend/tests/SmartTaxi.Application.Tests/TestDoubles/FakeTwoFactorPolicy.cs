using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTwoFactorPolicy : ITwoFactorPolicy
{
    public int RecoveryCodeCount { get; init; } = 8;

    public TimeSpan ChallengeTokenLifetime { get; init; } = TimeSpan.FromMinutes(5);

    public string Issuer { get; init; } = "SmartTaxi";
}
