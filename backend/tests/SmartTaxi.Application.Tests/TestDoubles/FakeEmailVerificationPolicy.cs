using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeEmailVerificationPolicy : IEmailVerificationPolicy
{
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan ResendInterval { get; init; } = TimeSpan.FromSeconds(60);
}
