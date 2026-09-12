namespace SmartTaxi.Application.Identity.Abstractions;

public interface ITwoFactorPolicy
{
    int RecoveryCodeCount { get; }

    TimeSpan ChallengeTokenLifetime { get; }

    string Issuer { get; }
}
