using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Infrastructure.Notifications.Services;

// Development/mock implementation only — no real push provider (Firebase/FCM/APNs) is integrated.
// Deliberately logs only a masked suffix of the device token, mirroring LoggingEmailSender/LoggingSmsSender.
internal sealed class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;

    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Push notification queued for device {MaskedToken} with title '{Title}'.", Mask(deviceToken), title);
        return Task.FromResult(true);
    }

    private static string Mask(string token) => token.Length <= 6 ? "***" : $"***{token[^6..]}";
}
