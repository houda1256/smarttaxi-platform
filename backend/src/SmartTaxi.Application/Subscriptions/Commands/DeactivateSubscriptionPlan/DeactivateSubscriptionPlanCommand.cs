using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.DeactivateSubscriptionPlan;

public sealed record DeactivateSubscriptionPlanCommand(Guid PlanId) : ICommand<Result>;
