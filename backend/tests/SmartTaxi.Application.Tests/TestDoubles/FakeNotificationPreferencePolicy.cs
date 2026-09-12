using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationPreferencePolicy : INotificationPreferencePolicy
{
    public NotificationChannel MandatoryChannels { get; set; } = NotificationChannel.Email | NotificationChannel.InApp;

    public NotificationChannel OptionalChannels { get; set; } = NotificationChannel.Email | NotificationChannel.InApp;

    public Task<IReadOnlyCollection<NotificationChannel>> ResolveChannelsAsync(
        Guid recipientUserId, bool isMandatory, CancellationToken cancellationToken)
    {
        var flags = isMandatory ? MandatoryChannels : OptionalChannels;
        IReadOnlyCollection<NotificationChannel> resolved =
            Enum.GetValues<NotificationChannel>().Where(value => value != NotificationChannel.None && flags.HasFlag(value)).ToList();

        return Task.FromResult(resolved);
    }
}
