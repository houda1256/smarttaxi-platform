using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionPlanById;

public sealed record GetSubscriptionPlanByIdQuery(Guid PlanId) : IQuery<SubscriptionPlan?>;
