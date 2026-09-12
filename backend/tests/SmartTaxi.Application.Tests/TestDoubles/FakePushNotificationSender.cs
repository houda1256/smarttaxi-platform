using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePushNotificationSender : IPushNotificationSender
{
    public List<(string DeviceToken, string Title, string Body)> SentMessages { get; } = [];

    public bool AlwaysFails { get; set; }

    public Task<bool> SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken)
    {
        if (AlwaysFails)
        {
            return Task.FromResult(false);
        }

        SentMessages.Add((deviceToken, title, body));
        return Task.FromResult(true);
    }
}
