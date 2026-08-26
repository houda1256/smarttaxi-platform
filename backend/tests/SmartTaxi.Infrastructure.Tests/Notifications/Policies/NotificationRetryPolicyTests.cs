using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Notifications.Options;
using SmartTaxi.Infrastructure.Notifications.Policies;

namespace SmartTaxi.Infrastructure.Tests.Notifications.Policies;

public class NotificationRetryPolicyTests
{
    private static NotificationRetryPolicy CreatePolicy(int maxAttempts = 3, int retryDelayMinutes = 15) =>
        new(Options.Create(new NotificationOptions { RetryMaxAttempts = maxAttempts, RetryDelayMinutes = retryDelayMinutes }));

    [Fact]
    public void ComputeNextAttempt_BelowMaxAttempts_ReturnsFutureTimestamp()
    {
        var policy = CreatePolicy(maxAttempts: 3);
        var utcNow = DateTime.UtcNow;

        var next = policy.ComputeNextAttempt(1, utcNow);

        Assert.NotNull(next);
        Assert.True(next > utcNow);
    }

    [Fact]
    public void ComputeNextAttempt_AtMaxAttempts_ReturnsNull()
    {
        var policy = CreatePolicy(maxAttempts: 3);

        var next = policy.ComputeNextAttempt(3, DateTime.UtcNow);

        Assert.Null(next);
    }

    [Fact]
    public void ComputeNextAttempt_AboveMaxAttempts_ReturnsNull()
    {
        var policy = CreatePolicy(maxAttempts: 3);

        var next = policy.ComputeNextAttempt(10, DateTime.UtcNow);

        Assert.Null(next);
    }
}
