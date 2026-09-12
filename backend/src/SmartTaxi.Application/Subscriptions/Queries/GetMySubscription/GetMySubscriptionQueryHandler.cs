using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetMySubscription;

public sealed class GetMySubscriptionQueryHandler : IQueryHandler<GetMySubscriptionQuery, Subscription?>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetMySubscriptionQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public Task<Subscription?> Handle(GetMySubscriptionQuery query, CancellationToken cancellationToken) =>
        _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(query.SubscriberId, query.TargetRole, cancellationToken);
}
