using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyChallengeTests
{
    private static LoyaltyChallenge NewChallenge(UserRole? eligibleRole = null, DateTime? validFrom = null, DateTime? validTo = null) =>
        LoyaltyChallenge.Create("TEN_RIDES", "10 courses", LoyaltyChallengeCriteriaType.RideCount, 10, 50, eligibleRole, validFrom, validTo, DateTime.UtcNow);

    [Fact]
    public void IsEffectiveFor_WithMatchingRoleAndActive_ReturnsTrue()
    {
        var challenge = NewChallenge(UserRole.Driver);

        Assert.True(challenge.IsEffectiveFor(UserRole.Driver, DateTime.UtcNow));
    }

    [Fact]
    public void IsEffectiveFor_WithNonMatchingRole_ReturnsFalse()
    {
        var challenge = NewChallenge(UserRole.Driver);

        Assert.False(challenge.IsEffectiveFor(UserRole.Customer, DateTime.UtcNow));
    }

    [Fact]
    public void IsEffectiveFor_WithNoEligibleRoleRestriction_AppliesToAnyRole()
    {
        var challenge = NewChallenge(eligibleRole: null);

        Assert.True(challenge.IsEffectiveFor(UserRole.Customer, DateTime.UtcNow));
        Assert.True(challenge.IsEffectiveFor(UserRole.Driver, DateTime.UtcNow));
    }

    [Fact]
    public void IsEffectiveFor_WhenDeactivated_ReturnsFalse()
    {
        var challenge = NewChallenge();
        challenge.Deactivate(DateTime.UtcNow);

        Assert.False(challenge.IsEffectiveFor(UserRole.Customer, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNonPositiveTargetValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyChallenge.Create(
            "CODE", "Name", LoyaltyChallengeCriteriaType.RideCount, 0, 50, null, null, null, DateTime.UtcNow));
    }
}
