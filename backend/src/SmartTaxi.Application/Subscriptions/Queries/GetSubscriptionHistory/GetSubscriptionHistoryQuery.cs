using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionHistory;

public sealed record GetSubscriptionHistoryQuery(Guid SubscriberId) : IQuery<IReadOnlyCollection<Subscription>>;
