using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed record CreateSubscriptionPlanCommand(
    string Name,
    string Code,
    string Description,
    UserRole TargetRole,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int TrialPeriodDays,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits) : ICommand<Result<Guid>>;
