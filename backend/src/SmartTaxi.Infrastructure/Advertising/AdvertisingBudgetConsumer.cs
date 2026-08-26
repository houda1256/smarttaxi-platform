using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising;

/// <summary>
/// Shared by AdCampaignRepository.TryConsumeBudgetAsync and the
/// Impression/Click repositories' atomic TryRecordAsync — a single
/// conditional UPDATE that never loads/checks/saves in separate steps (Module
/// 8 audit's mandatory budget-concurrency rule). Guards, all in the same
/// statement: the campaign is Active, the total remaining budget absorbs the
/// amount, and (when a daily limit is configured) the remaining DAILY budget
/// absorbs it too — the daily counter resets automatically whenever the
/// stored DailyConsumedDateUtc no longer matches today's UTC date, expressed
/// as a SQL CASE via the ternary below. Must be called within the caller's
/// own open transaction if it also mutates other tables in the same logical
/// operation (e.g. inserting the impression/click row) — this method itself
/// does not open a transaction.
/// </summary>
internal enum BudgetConsumptionOutcome
{
    Consumed,
    CampaignNotActive,
    BudgetExceeded
}

internal static class AdvertisingBudgetConsumer
{
    public static async Task<BudgetConsumptionOutcome> TryConsumeAsync(
        ApplicationDbContext context, Guid campaignId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Le montant à consommer ne peut pas être négatif.");
        }

        if (amount == 0)
        {
            // Zero-cost fact (Flat/Duration/Cpc impression, or Cpm click) — still requires the campaign to
            // be genuinely Active and within its own schedule (defense-in-depth: Status can lag briefly
            // behind EndAtUtc until the next ProcessCompletedCampaigns sweep — Module 8 audit LOW finding).
            var isActive = await context.AdCampaigns
                .AnyAsync(c => c.Id == campaignId && c.Status == AdCampaignStatus.Active && c.EndAtUtc > utcNow, cancellationToken);
            return isActive ? BudgetConsumptionOutcome.Consumed : BudgetConsumptionOutcome.CampaignNotActive;
        }

        var today = utcNow.Date;

        var rows = await context.AdCampaigns
            .Where(c => c.Id == campaignId && c.Status == AdCampaignStatus.Active && c.EndAtUtc > utcNow
                && c.ConsumedBudget + amount <= c.BudgetLimit
                && (c.DailyBudgetLimit == null
                    || (c.DailyConsumedDateUtc == today ? c.DailyConsumedBudget + amount : amount) <= c.DailyBudgetLimit))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ConsumedBudget, c => c.ConsumedBudget + amount)
                .SetProperty(c => c.DailyConsumedBudget, c => c.DailyConsumedDateUtc == today ? c.DailyConsumedBudget + amount : amount)
                .SetProperty(c => c.DailyConsumedDateUtc, today)
                .SetProperty(c => c.UpdatedAtUtc, utcNow), cancellationToken);

        if (rows == 1)
        {
            return BudgetConsumptionOutcome.Consumed;
        }

        // Best-effort diagnostic read to report an accurate reason — the atomic guarantee above already
        // stands regardless of what this follow-up read observes (it can never itself cause an overrun).
        var stillActive = await context.AdCampaigns
            .AnyAsync(c => c.Id == campaignId && c.Status == AdCampaignStatus.Active && c.EndAtUtc > utcNow, cancellationToken);
        return stillActive ? BudgetConsumptionOutcome.BudgetExceeded : BudgetConsumptionOutcome.CampaignNotActive;
    }
}
