using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    Guid PlanId,
    string Name,
    string Description,
    decimal Price,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits) : ICommand<Result>;
