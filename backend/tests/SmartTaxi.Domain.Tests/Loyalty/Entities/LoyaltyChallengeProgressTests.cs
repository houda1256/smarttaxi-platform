using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Tests.Loyalty.Entities;

public class LoyaltyChallengeProgressTests
{
    [Fact]
    public void UpdateProgress_ReachingTarget_RaisesChallengeCompleted()
    {
        var progress = LoyaltyChallengeProgress.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        progress.UpdateProgress(10, 10, DateTime.UtcNow);

        Assert.True(progress.IsCompleted);
        Assert.Contains(progress.DomainEvents, e => e is ChallengeCompleted);
    }

    [Fact]
    public void UpdateProgress_BelowTarget_DoesNotComplete()
    {
        var progress = LoyaltyChallengeProgress.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        progress.UpdateProgress(5, 10, DateTime.UtcNow);

        Assert.False(progress.IsCompleted);
    }

    [Fact]
    public void UpdateProgress_Regression_Throws()
    {
        var progress = LoyaltyChallengeProgress.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        progress.UpdateProgress(5, 10, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => progress.UpdateProgress(3, 10, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateProgress_AlreadyCompleted_DoesNotRaiseSecondEvent()
    {
        var progress = LoyaltyChallengeProgress.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        progress.UpdateProgress(10, 10, DateTime.UtcNow);

        progress.UpdateProgress(15, 10, DateTime.UtcNow);

        Assert.Single(progress.DomainEvents, e => e is ChallengeCompleted);
    }

    [Fact]
    public void MarkRewarded_CalledTwice_IsIdempotent()
    {
        var progress = LoyaltyChallengeProgress.Start(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var firstRewardTime = DateTime.UtcNow;
        progress.MarkRewarded(firstRewardTime);

        progress.MarkRewarded(firstRewardTime.AddMinutes(5));

        Assert.Equal(firstRewardTime, progress.RewardedAtUtc);
    }
}
