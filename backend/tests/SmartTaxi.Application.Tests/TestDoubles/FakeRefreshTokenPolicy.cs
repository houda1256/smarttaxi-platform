using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRefreshTokenPolicy : IRefreshTokenPolicy
{
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan SessionLifetime { get; init; } = TimeSpan.FromDays(90);
}
