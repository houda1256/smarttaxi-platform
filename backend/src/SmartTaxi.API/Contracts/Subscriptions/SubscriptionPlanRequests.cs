using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.API.Contracts.Subscriptions;

public sealed record CreateSubscriptionPlanRequest(
    string Name,
    string Code,
    string Description,
    UserRole TargetRole,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int TrialPeriodDays,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits);

public sealed record UpdateSubscriptionPlanRequest(
    string Name,
    string Description,
    decimal Price,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits);
