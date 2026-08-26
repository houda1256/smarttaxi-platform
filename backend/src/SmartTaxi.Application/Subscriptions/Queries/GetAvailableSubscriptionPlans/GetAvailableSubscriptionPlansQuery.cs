using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetAvailableSubscriptionPlans;

public sealed record GetAvailableSubscriptionPlansQuery(UserRole TargetRole) : IQuery<IReadOnlyCollection<SubscriptionPlan>>;
