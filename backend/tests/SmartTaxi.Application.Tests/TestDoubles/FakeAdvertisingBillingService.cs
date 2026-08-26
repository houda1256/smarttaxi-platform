using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdvertisingBillingService : IAdvertisingBillingService
{
    private readonly HashSet<Guid> _settledCampaignIds = [];

    public List<(Guid CampaignId, Guid AdvertiserUserId, decimal Amount)> SettledCalls { get; } = [];

    /// <summary>Simulates the Fix 2 partial-failure scenario: the ledger entry exists (as if PostBatchAsync had
    /// already committed in a prior, crashed attempt) even though TrySettleCampaignAsync will report "already
    /// settled" (false) — proving the recovery path converges instead of silently failing forever.</summary>
    public HashSet<Guid> CampaignIdsWithOrphanedLedgerEntry { get; } = [];

    public Task<bool> TrySettleCampaignAsync(Guid campaignId, Guid advertiserUserId, decimal amount, string currency, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (CampaignIdsWithOrphanedLedgerEntry.Contains(campaignId) || !_settledCampaignIds.Add(campaignId))
        {
            // Mirrors the real ledger's own unique-index idempotency guard rejecting a duplicate post —
            // never records a second SettledCalls entry.
            return Task.FromResult(false);
        }

        SettledCalls.Add((campaignId, advertiserUserId, amount));
        return Task.FromResult(true);
    }

    public Task<bool> HasSettlementEntryAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_settledCampaignIds.Contains(campaignId) || CampaignIdsWithOrphanedLedgerEntry.Contains(campaignId));
}
