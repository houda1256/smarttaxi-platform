using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionHistory;

public sealed class GetSubscriptionHistoryQueryHandler : IQueryHandler<GetSubscriptionHistoryQuery, IReadOnlyCollection<Subscription>>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetSubscriptionHistoryQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public Task<IReadOnlyCollection<Subscription>> Handle(GetSubscriptionHistoryQuery query, CancellationToken cancellationToken) =>
        _subscriptionRepository.GetHistoryForSubscriberAsync(query.SubscriberId, cancellationToken);
}
