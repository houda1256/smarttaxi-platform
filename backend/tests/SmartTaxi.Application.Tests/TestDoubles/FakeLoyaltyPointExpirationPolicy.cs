using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyPointExpirationPolicy : ILoyaltyPointExpirationPolicy
{
    public int ExpirationMonths { get; set; } = 12;
}
