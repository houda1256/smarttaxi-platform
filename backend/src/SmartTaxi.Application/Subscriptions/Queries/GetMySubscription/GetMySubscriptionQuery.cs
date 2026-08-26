using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetMySubscription;

public sealed record GetMySubscriptionQuery(Guid SubscriberId, UserRole TargetRole) : IQuery<Subscription?>;
