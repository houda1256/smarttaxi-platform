using Microsoft.Extensions.Logging.Abstractions;
using SmartTaxi.Infrastructure.Notifications.Services;

namespace SmartTaxi.Infrastructure.Tests.Notifications.Services;

public class LoggingPushNotificationSenderTests
{
    [Fact]
    public async Task SendAsync_AlwaysReturnsTrue()
    {
        var sender = new LoggingPushNotificationSender(NullLogger<LoggingPushNotificationSender>.Instance);

        var result = await sender.SendAsync("device-token-1234567890", "Title", "Body", CancellationToken.None);

        Assert.True(result);
    }
}
