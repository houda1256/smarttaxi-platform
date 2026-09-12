using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyPointLedgerEntryTests
{
    private static LoyaltyPointLedgerEntry NewEntry(Guid? userId = null) => LoyaltyPointLedgerEntry.Create(
        Guid.NewGuid(), userId ?? Guid.NewGuid(), LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 10, 10, "Payment",
        Guid.NewGuid(), "Trajet payé", DateTime.UtcNow);

    [Fact]
    public void Create_WithNegativeBalanceAfter_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyPointLedgerEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, -10, -1, "Redemption",
            Guid.NewGuid(), "reason", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithBlankReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => LoyaltyPointLedgerEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn, 10, 10, "Payment",
            Guid.NewGuid(), " ", DateTime.UtcNow));
    }

    [Fact]
    public void ComputeIdempotencyKey_SameInputs_ProducesSameKey()
    {
        var sourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var first = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("Payment", sourceId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn);
        var second = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("Payment", sourceId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeIdempotencyKey_SameSourceDifferentUser_ProducesDifferentKeys()
    {
        // The exact scenario a referral reward fans out into: one SourceId (the Referral), two different recipients.
        var sourceId = Guid.NewGuid();

        var referrerKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("Referral", sourceId, Guid.NewGuid(), LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ReferralReward);
        var refereeKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("Referral", sourceId, Guid.NewGuid(), LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ReferralReward);

        Assert.NotEqual(referrerKey, refereeKey);
    }

    [Fact]
    public void ComputeIdempotencyKey_DifferentEntryType_ProducesDifferentKey()
    {
        var sourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var earnKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("LoyaltyPointLedgerEntry", sourceId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Earn);
        var expireKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey("LoyaltyPointLedgerEntry", sourceId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire);

        Assert.NotEqual(earnKey, expireKey);
    }
}
