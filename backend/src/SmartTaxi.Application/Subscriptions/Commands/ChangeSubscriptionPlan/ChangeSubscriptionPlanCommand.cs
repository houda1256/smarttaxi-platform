using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.ChangeSubscriptionPlan;

/// <summary>Covers both upgrade and downgrade — same mechanism, direction only depends on the two plans' relative price.</summary>
public sealed record ChangeSubscriptionPlanCommand(Guid SubscriptionId, Guid RequestingUserId, Guid NewPlanId) : ICommand<Result>;
