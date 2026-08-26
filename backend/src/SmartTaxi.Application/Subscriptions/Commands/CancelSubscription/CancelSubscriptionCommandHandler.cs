using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler : ICommandHandler<CancelSubscriptionCommand, Result>
{
    private const string NotFoundError = "Abonnement introuvable.";
    private const string NotOwnerError = "Seul le titulaire peut annuler cet abonnement.";
    private const string CancellationConflictError = "Cet abonnement ne peut plus être annulé.";

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository, INotificationDispatcher notificationDispatcher)
    {
        _subscriptionRepository = subscriptionRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(command.SubscriptionId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (subscription.SubscriberId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var cancelled = await _subscriptionRepository.TryCancelAsync(subscription.Id, DateTime.UtcNow, cancellationToken);

        if (!cancelled)
        {
            return Result.Failure(CancellationConflictError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                subscription.SubscriberId, NotificationCategory.Subscription, "subscription.cancelled", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "Subscription", SourceId: subscription.Id),
            cancellationToken);

        return Result.Success();
    }
}
