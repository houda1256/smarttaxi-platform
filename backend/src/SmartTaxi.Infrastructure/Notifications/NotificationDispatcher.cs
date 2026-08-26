using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Infrastructure.Notifications;

/// <summary>
/// The concrete implementation behind INotificationDispatcher — the one seam
/// business modules call. Lives in Infrastructure (not Application, unlike
/// most Application-layer services) purely because it needs ILogger — the
/// Application project takes no NuGet package references at all (only a
/// ProjectReference to Domain), so any type needing Microsoft.Extensions.*
/// belongs here instead, same reasoning as JwtTokenGenerator/PayoutPolicy.
/// Deliberately swallows every exception (logging, never rethrowing): a
/// notification failure must never fail the business transaction that
/// triggered it (a confirmed payment, a completed ride, an approved driver are
/// all authoritative regardless of whether notifying the user about it
/// succeeded). Idempotent by construction — see Notification.ComputeIdempotencyKey
/// and INotificationRepository.TryAddAsync.
/// </summary>
internal sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDeliveryAttemptRepository _attemptRepository;
    private readonly INotificationPreferencePolicy _preferencePolicy;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly INotificationRetryPolicy _retryPolicy;
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IPushNotificationSender _pushSender;
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly INotificationRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationRepository notificationRepository, INotificationDeliveryAttemptRepository attemptRepository,
        INotificationPreferencePolicy preferencePolicy, INotificationTemplateRenderer renderer, INotificationRetryPolicy retryPolicy,
        IUserRepository userRepository, IUserPreferencesRepository preferencesRepository, IEmailSender emailSender,
        ISmsSender smsSender, IPushNotificationSender pushSender, IDeviceTokenRepository deviceTokenRepository,
        INotificationRealtimeNotifier realtimeNotifier, ILogger<NotificationDispatcher> logger)
    {
        _notificationRepository = notificationRepository;
        _attemptRepository = attemptRepository;
        _preferencePolicy = preferencePolicy;
        _renderer = renderer;
        _retryPolicy = retryPolicy;
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _pushSender = pushSender;
        _deviceTokenRepository = deviceTokenRepository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var idempotencyKey = Notification.ComputeIdempotencyKey(
                request.SourceType, request.SourceId, request.RecipientUserId, request.Category);

            if (await _notificationRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken) is not null)
            {
                return;
            }

            var channels = await _preferencePolicy.ResolveChannelsAsync(request.RecipientUserId, request.IsMandatory, cancellationToken);

            if (channels.Count == 0)
            {
                _logger.LogInformation(
                    "Notification {TemplateKey} for {RecipientUserId} skipped: no channel resolved (preferences opted out).",
                    request.TemplateKey, request.RecipientUserId);
                return;
            }

            var language = await ResolveLanguageAsync(request.RecipientUserId, cancellationToken);
            var primaryChannel = channels.Contains(NotificationChannel.InApp) ? NotificationChannel.InApp : channels.First();
            var primaryContent = await _renderer.RenderAsync(
                request.TemplateKey, primaryChannel, language, request.Variables, cancellationToken);

            var notification = Notification.Create(
                request.RecipientUserId, request.Category, request.TemplateKey, primaryContent.Subject ?? request.TemplateKey,
                primaryContent.Body, request.Variables, request.IsMandatory, request.SourceType, request.SourceId, DateTime.UtcNow);

            if (!await _notificationRepository.TryAddAsync(notification, cancellationToken))
            {
                // Lost the idempotency race to a concurrent dispatch of the same event for the same recipient/category.
                return;
            }

            var user = await _userRepository.GetByIdAsync(request.RecipientUserId, cancellationToken);

            foreach (var channel in channels)
            {
                await DispatchChannelAsync(notification, channel, language, request.Variables, user, attemptNumber: 1, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Notification dispatch failed for template {TemplateKey}, recipient {RecipientUserId}.",
                request.TemplateKey, request.RecipientUserId);
        }
    }

    public async Task RetryFailedDeliveryAsync(Guid deliveryAttemptId, CancellationToken cancellationToken)
    {
        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(deliveryAttemptId, cancellationToken);

            if (attempt is null || attempt.Status != NotificationDeliveryStatus.Failed)
            {
                return;
            }

            var notification = await _notificationRepository.GetByIdAsync(attempt.NotificationId, cancellationToken);

            if (notification is null)
            {
                return;
            }

            var language = await ResolveLanguageAsync(notification.RecipientUserId, cancellationToken);
            var user = await _userRepository.GetByIdAsync(notification.RecipientUserId, cancellationToken);

            await DispatchChannelAsync(
                notification, attempt.Channel, language, notification.Variables, user, attempt.AttemptNumber + 1, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification delivery retry failed for attempt {DeliveryAttemptId}.", deliveryAttemptId);
        }
    }

    private async Task<Language> ResolveLanguageAsync(Guid recipientUserId, CancellationToken cancellationToken)
    {
        var preferences = await _preferencesRepository.GetByUserIdAsync(recipientUserId, cancellationToken);
        return preferences?.Language ?? Language.French;
    }

    private async Task DispatchChannelAsync(
        Notification notification, NotificationChannel channel, Language language, IReadOnlyDictionary<string, string> variables,
        User? user, int attemptNumber, CancellationToken cancellationToken)
    {
        switch (channel)
        {
            case NotificationChannel.InApp:
                await SendInAppAsync(notification, attemptNumber, cancellationToken);
                break;

            case NotificationChannel.Email:
                if (user is not null)
                {
                    await SendEmailAsync(notification, user.Email.Value, language, variables, attemptNumber, cancellationToken);
                }

                break;

            case NotificationChannel.Sms:
                if (user?.PhoneNumber is not null)
                {
                    await SendSmsAsync(notification, user.PhoneNumber, language, variables, attemptNumber, cancellationToken);
                }

                break;

            case NotificationChannel.Push:
                await SendPushAsync(notification, attemptNumber, cancellationToken);
                break;
        }
    }

    /// <summary>Persisting the Notification row already is the InApp delivery — this attempt records it as Sent immediately, then a realtime push is attempted best-effort (see the type-level doc comment on INotificationRealtimeNotifier).</summary>
    private async Task SendInAppAsync(Notification notification, int attemptNumber, CancellationToken cancellationToken)
    {
        var attempt = NotificationDeliveryAttempt.Create(notification.Id, NotificationChannel.InApp, attemptNumber, DateTime.UtcNow);
        attempt.MarkSent(DateTime.UtcNow, providerMessageId: null);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        try
        {
            await _realtimeNotifier.NotifyNewNotificationAsync(
                notification.RecipientUserId, notification.Id, notification.Category.ToString(), notification.Title, cancellationToken);

            var unreadCount = await _notificationRepository.GetUnreadCountAsync(notification.RecipientUserId, cancellationToken);
            await _realtimeNotifier.NotifyUnreadCountChangedAsync(notification.RecipientUserId, unreadCount, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Realtime push failed for notification {NotificationId} — the notification remains stored.", notification.Id);
        }
    }

    private async Task SendEmailAsync(
        Notification notification, string email, Language language, IReadOnlyDictionary<string, string> variables,
        int attemptNumber, CancellationToken cancellationToken)
    {
        var attempt = NotificationDeliveryAttempt.Create(notification.Id, NotificationChannel.Email, attemptNumber, DateTime.UtcNow);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        try
        {
            var content = await _renderer.RenderAsync(notification.TemplateKey, NotificationChannel.Email, language, variables, cancellationToken);
            await _emailSender.SendAsync(email, content.Subject ?? notification.Title, content.Body, cancellationToken);
            attempt.MarkSent(DateTime.UtcNow, providerMessageId: null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email delivery failed for notification {NotificationId}.", notification.Id);
            attempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", SanitizedFailureMessage, _retryPolicy.ComputeNextAttempt(attemptNumber, DateTime.UtcNow));
        }

        await _attemptRepository.UpdateAsync(attempt, cancellationToken);
    }

    private async Task SendSmsAsync(
        Notification notification, string phoneNumber, Language language, IReadOnlyDictionary<string, string> variables,
        int attemptNumber, CancellationToken cancellationToken)
    {
        var attempt = NotificationDeliveryAttempt.Create(notification.Id, NotificationChannel.Sms, attemptNumber, DateTime.UtcNow);
        await _attemptRepository.AddAsync(attempt, cancellationToken);

        try
        {
            var content = await _renderer.RenderAsync(notification.TemplateKey, NotificationChannel.Sms, language, variables, cancellationToken);
            await _smsSender.SendAsync(phoneNumber, content.Body, cancellationToken);
            attempt.MarkSent(DateTime.UtcNow, providerMessageId: null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS delivery failed for notification {NotificationId}.", notification.Id);
            attempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", SanitizedFailureMessage, _retryPolicy.ComputeNextAttempt(attemptNumber, DateTime.UtcNow));
        }

        await _attemptRepository.UpdateAsync(attempt, cancellationToken);
    }

    /// <summary>One delivery attempt row per active device — a token registered on a second phone must not silently swallow a push to the first.</summary>
    private async Task SendPushAsync(Notification notification, int attemptNumber, CancellationToken cancellationToken)
    {
        var tokens = await _deviceTokenRepository.GetActiveForUserAsync(notification.RecipientUserId, cancellationToken);

        foreach (var token in tokens)
        {
            var attempt = NotificationDeliveryAttempt.Create(notification.Id, NotificationChannel.Push, attemptNumber, DateTime.UtcNow);
            await _attemptRepository.AddAsync(attempt, cancellationToken);

            try
            {
                var accepted = await _pushSender.SendAsync(token.Token, notification.Title, notification.Body, cancellationToken);

                if (accepted)
                {
                    attempt.MarkSent(DateTime.UtcNow, providerMessageId: null);
                }
                else
                {
                    attempt.MarkFailed(
                        DateTime.UtcNow, "PROVIDER_REJECTED", "Le fournisseur a refusé l'envoi.",
                        _retryPolicy.ComputeNextAttempt(attemptNumber, DateTime.UtcNow));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push delivery failed for notification {NotificationId}.", notification.Id);
                attempt.MarkFailed(DateTime.UtcNow, "SEND_FAILED", SanitizedFailureMessage, _retryPolicy.ComputeNextAttempt(attemptNumber, DateTime.UtcNow));
            }

            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
        }
    }

    /// <summary>Never persists raw exception text — it could carry provider internals; the full exception is only ever logged server-side above.</summary>
    private const string SanitizedFailureMessage = "L'envoi a échoué. Voir les journaux serveur pour le détail.";
}
