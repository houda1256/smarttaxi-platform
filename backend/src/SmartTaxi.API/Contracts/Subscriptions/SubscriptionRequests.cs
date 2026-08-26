namespace SmartTaxi.API.Contracts.Subscriptions;

public sealed record SubscribeRequest(Guid PlanId, bool AutoRenew);

public sealed record ChangeSubscriptionPlanRequest(Guid NewPlanId);
