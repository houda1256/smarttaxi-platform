using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSecurityPolicy : ISecurityPolicy
{
    public bool RevokeOtherSessionsOnPasswordChange { get; init; } = true;
}
