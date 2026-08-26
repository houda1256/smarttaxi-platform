using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Application.Advertising;

/// <summary>
/// Concrete Finance integration (lives in Application, like
/// LedgerPostingService/RevenueSharingCalculator, since it only composes
/// Application-layer Payments repositories — no Infrastructure dependency).
/// Single-line posting: Debit=Advertiser (Debt bucket, they now owe this
/// amount), Credit=Platform (Available bucket, revenue recognized) — the
/// mirror image of a Refund's single-line Available debit. Keyed by
/// (SourceType="AdvertisingCampaign", SourceId=campaignId,
/// EntryType=AdvertisingRevenue) so a campaign can only ever be settled once
/// through this seam; incremental/partial settlement is explicitly out of
/// scope for this pass.
/// </summary>
public sealed class AdvertisingBillingService : IAdvertisingBillingService
{
    private const string CampaignSourceType = "AdvertisingCampaign";

    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IFinancialLedgerRepository _ledgerRepository;

    public AdvertisingBillingService(IFinancialAccountRepository accountRepository, IFinancialLedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<bool> TrySettleCampaignAsync(
        Guid campaignId, Guid advertiserUserId, decimal amount, string currency, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return false;
        }

        var advertiserAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Advertiser, advertiserUserId, currency, cancellationToken);
        var platformAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, currency, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            new(advertiserAccount.Id, platformAccount.Id, amount, LedgerEntryType.AdvertisingRevenue, "Facturation budget publicitaire consommé",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Debt, amount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, amount)])
        };

        return await _ledgerRepository.PostBatchAsync(CampaignSourceType, campaignId, currency, null, utcNow, lines, cancellationToken);
    }

    public async Task<bool> HasSettlementEntryAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var entries = await _ledgerRepository.GetForSourceAsync(CampaignSourceType, campaignId, cancellationToken);
        return entries.Any(entry => entry.EntryType == LedgerEntryType.AdvertisingRevenue);
    }
}
