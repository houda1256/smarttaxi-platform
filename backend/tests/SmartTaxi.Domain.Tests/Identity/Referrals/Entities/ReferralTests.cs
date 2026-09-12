using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Referrals.Entities;

public class ReferralTests
{
    [Fact]
    public void Constructor_SetsInitialStateAsPendingActivation()
    {
        var referrer = Guid.NewGuid();
        var referee = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var referral = new Referral(referrer, referee, "CODE123", utcNow);

        Assert.Equal(referrer, referral.ReferrerUserId);
        Assert.Equal(referee, referral.RefereeUserId);
        Assert.Equal("CODE123", referral.ReferralCodeUsed);
        Assert.Equal(ReferralStatus.PendingActivation, referral.Status);
        Assert.Null(referral.ActivatedAt);
    }
}
