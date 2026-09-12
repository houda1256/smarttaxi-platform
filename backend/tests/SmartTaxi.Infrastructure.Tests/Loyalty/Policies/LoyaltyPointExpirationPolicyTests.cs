using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Loyalty.Options;
using SmartTaxi.Infrastructure.Loyalty.Policies;

namespace SmartTaxi.Infrastructure.Tests.Loyalty.Policies;

public class LoyaltyPointExpirationPolicyTests
{
    [Fact]
    public void ExpirationMonths_ReflectsConfiguredOptions()
    {
        var policy = new LoyaltyPointExpirationPolicy(Options.Create(new LoyaltyOptions { PointExpirationMonths = 6 }));

        Assert.Equal(6, policy.ExpirationMonths);
    }
}
