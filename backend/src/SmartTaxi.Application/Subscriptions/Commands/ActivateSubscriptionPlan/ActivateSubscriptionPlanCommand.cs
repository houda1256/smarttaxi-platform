using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.ActivateSubscriptionPlan;

public sealed record ActivateSubscriptionPlanCommand(Guid PlanId) : ICommand<Result>;
