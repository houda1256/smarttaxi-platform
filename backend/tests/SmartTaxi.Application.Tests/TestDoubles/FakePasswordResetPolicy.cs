using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePasswordResetPolicy : IPasswordResetPolicy
{
    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(1);
}
