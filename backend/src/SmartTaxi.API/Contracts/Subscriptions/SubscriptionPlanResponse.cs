using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.API.Contracts.Subscriptions;

public sealed record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    string Code,
    string Description,
    UserRole TargetRole,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    int TrialPeriodDays,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static SubscriptionPlanResponse FromEntity(SubscriptionPlan plan) => new(
        plan.Id, plan.Name, plan.Code, plan.Description, plan.TargetRole, plan.Price, plan.Currency, plan.BillingPeriod,
        plan.TrialPeriodDays, plan.Features, plan.Limits, plan.IsActive, plan.CreatedAt, plan.UpdatedAt);
}
