namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>
/// The Finance integration seam (Module 8 audit, re-verified: Payments'
/// FinancialAccount/FinancialLedgerEntry ledger IS operational — see
/// AdvertisingBillingService). Posts what the advertiser owes the Platform for
/// a campaign's settled consumed budget as one ledger entry; it never creates
/// a second financial ledger, wallet, or balance of its own. Actually
/// collecting real money from the advertiser (charging a card/bank transfer)
/// remains explicitly deferred, exactly like Subscriptions' own
/// DevSubscriptionChargeCollector — this only records what is owed.
/// </summary>
public interface IAdvertisingBillingService
{
    /// <summary>Returns false if this campaign was already settled (idempotent via the ledger's own unique index on (SourceType, SourceId, EntryType)) — a false result is a harmless no-op, not an error.</summary>
    Task<bool> TrySettleCampaignAsync(
        Guid campaignId, Guid advertiserUserId, decimal amount, string currency, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Recovery check for the Module 8 audit's settlement-convergence fix: does the settlement ledger
    /// entry for this campaign genuinely exist? Used only when TrySettleCampaignAsync reports the source
    /// already posted — distinguishes "a previous attempt's ledger post committed but the campaign's own
    /// SettledAtUtc flag never got set" (safe to converge) from any other reason a post could fail. Never
    /// posts anything itself — read-only.
    /// </summary>
    Task<bool> HasSettlementEntryAsync(Guid campaignId, CancellationToken cancellationToken);
}
