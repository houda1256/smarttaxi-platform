using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetAllSubscriptionPlans;

public sealed record GetAllSubscriptionPlansQuery(int PageNumber, int PageSize) : IQuery<PagedResult<SubscriptionPlan>>;
