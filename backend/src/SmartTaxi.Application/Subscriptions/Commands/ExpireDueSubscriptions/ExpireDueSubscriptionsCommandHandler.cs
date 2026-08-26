using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.ExpireDueSubscriptions;

public sealed class ExpireDueSubscriptionsCommandHandler : ICommandHandler<ExpireDueSubscriptionsCommand, Result<int>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ExpireDueSubscriptionsCommandHandler(ISubscriptionRepository subscriptionRepository, INotificationDispatcher notificationDispatcher)
    {
        _subscriptionRepository = subscriptionRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<int>> Handle(ExpireDueSubscriptionsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var due = await _subscriptionRepository.GetActiveExpiredAsOfAsync(utcNow, cancellationToken);

        var expiredCount = 0;

        foreach (var subscription in due)
        {
            if (await _subscriptionRepository.TryExpireAsync(subscription.Id, utcNow, cancellationToken))
            {
                expiredCount++;

                await _notificationDispatcher.DispatchAsync(
                    new NotificationRequest(
                        subscription.SubscriberId, NotificationCategory.Subscription, "subscription.expired",
                        new Dictionary<string, string>(), IsMandatory: false, SourceType: "Subscription", SourceId: subscription.Id),
                    cancellationToken);
            }
        }

        return Result<int>.Success(expiredCount);
    }
}
