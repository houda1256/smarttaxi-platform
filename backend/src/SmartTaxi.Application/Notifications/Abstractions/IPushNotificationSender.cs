namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>
/// Dependency-inversion boundary mirroring IEmailSender/ISmsSender — the only
/// implementation registered today is a development/logging stub (no Firebase/
/// FCM credentials anywhere in this codebase). Returns whether the provider
/// call itself succeeded, never a real transport-delivery guarantee.
/// </summary>
public interface IPushNotificationSender
{
    Task<bool> SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken);
}
