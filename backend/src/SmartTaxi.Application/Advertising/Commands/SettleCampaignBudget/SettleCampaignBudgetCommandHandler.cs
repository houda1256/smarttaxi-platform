using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;

/// <summary>
/// Explicit, admin-triggered Finance handoff — posts what the advertiser owes
/// for the campaign's consumed budget as one ledger entry (see
/// IAdvertisingBillingService). One-shot: a campaign can only ever be settled
/// once through this seam — incremental/partial settlement during an Active
/// campaign's lifetime is explicitly out of scope for this pass; only
/// Completed/Cancelled/Suspended campaigns (nothing left to consume) are
/// eligible.
///
/// Recovery/convergence (Module 8 audit fix #2): the ledger post
/// (IFinancialLedgerRepository.PostBatchAsync, committed independently inside
/// AdvertisingBillingService) and this campaign's own SettledAtUtc flag are
/// two separate writes with no shared transaction — a crash between them
/// would otherwise leave SettledAtUtc permanently null while the ledger
/// already reflects the charge, with every retry hitting the ledger's own
/// idempotency guard and returning a stale-looking "already settled" Conflict
/// forever. When TrySettleCampaignAsync reports the source already exists, we
/// now check (via the existing, unmodified IAdvertisingBillingService/
/// IFinancialLedgerRepository read surface — no Finance abstraction changed)
/// whether the expected AdvertisingRevenue entry genuinely exists; if it
/// does, this is recovery from that exact partial-failure window, and the
/// campaign converges to Settled without ever posting a second ledger entry.
/// </summary>
public sealed class SettleCampaignBudgetCommandHandler : ICommandHandler<SettleCampaignBudgetCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotEligibleError = "Cette campagne n'est pas éligible à la facturation dans son état actuel.";
    private const string NothingConsumedError = "Aucun budget consommé à facturer.";
    private const string AlreadySettledError = "Cette campagne a déjà été facturée.";

    private static readonly AdCampaignStatus[] EligibleStatuses = [AdCampaignStatus.Completed, AdCampaignStatus.Cancelled, AdCampaignStatus.Suspended];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingBillingService _billingService;

    public SettleCampaignBudgetCommandHandler(IAdCampaignRepository campaignRepository, IAdvertisingBillingService billingService)
    {
        _campaignRepository = campaignRepository;
        _billingService = billingService;
    }

    public async Task<Result> Handle(SettleCampaignBudgetCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (!EligibleStatuses.Contains(campaign.Status))
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        // A. Already settled — unchanged API semantics, no ledger/recovery work needed.
        if (campaign.SettledAtUtc is not null)
        {
            return Result.Failure(AlreadySettledError, ErrorType.Conflict);
        }

        if (campaign.ConsumedBudget <= 0)
        {
            return Result.Failure(NothingConsumedError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;

        // B/C. Normal path: attempt the ledger posting, then mark settled.
        var posted = await _billingService.TrySettleCampaignAsync(
            campaign.Id, campaign.AdvertiserUserId, campaign.ConsumedBudget, "TND", utcNow, cancellationToken);

        if (posted)
        {
            await _campaignRepository.TryMarkSettledAsync(campaign.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        // D/E. The deterministic (SourceType="AdvertisingCampaign", SourceId=campaignId) source already
        // exists — verify the EXPECTED entry (EntryType=AdvertisingRevenue) is really there before treating
        // this as recovery, never a second posting (F) and never a false-positive from an unrelated entry (G).
        if (await _billingService.HasSettlementEntryAsync(campaign.Id, cancellationToken))
        {
            await _campaignRepository.TryMarkSettledAsync(campaign.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        return Result.Failure(AlreadySettledError, ErrorType.Conflict);
    }
}
