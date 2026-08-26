using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance;

/// <summary>
/// Concrete Finance integration (lives in Application, like
/// MaintenanceBillingService/AdvertisingBillingService, since it only
/// composes Application-layer Payments repositories — no Infrastructure
/// dependency). Single-line posting: Debit=the requester's own account
/// (TaxiOwner or Driver, resolved from RequesterRole — approved plan, Q2),
/// Credit=RoadsideAssistancePartner — no platform commission, the full
/// settled amount goes to the partner. Keyed by
/// (SourceType="RoadsideAssistanceRequest", SourceId=requestId,
/// EntryType=RoadsideAssistanceRevenue) so a request can only ever be settled
/// once through this seam.
/// </summary>
public sealed class RoadsideAssistanceBillingService : IRoadsideAssistanceBillingService
{
    private const string RequestSourceType = "RoadsideAssistanceRequest";

    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IFinancialLedgerRepository _ledgerRepository;

    public RoadsideAssistanceBillingService(IFinancialAccountRepository accountRepository, IFinancialLedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<bool> TrySettleRequestAsync(
        Guid requestId, Guid requesterUserId, RoadsideRequesterRole requesterRole, Guid partnerUserId, decimal amount, string currency,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return false;
        }

        var payerAccountType = requesterRole == RoadsideRequesterRole.TaxiOwner ? FinancialAccountType.TaxiOwner : FinancialAccountType.Driver;

        var requesterAccount = await _accountRepository.GetOrCreateAsync(payerAccountType, requesterUserId, currency, cancellationToken);
        var partnerAccount = await _accountRepository.GetOrCreateAsync(
            FinancialAccountType.RoadsideAssistancePartner, partnerUserId, currency, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            new(requesterAccount.Id, partnerAccount.Id, amount, LedgerEntryType.RoadsideAssistanceRevenue, "Règlement assistance routière",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Debt, amount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, amount)])
        };

        return await _ledgerRepository.PostBatchAsync(RequestSourceType, requestId, currency, null, utcNow, lines, cancellationToken);
    }

    public async Task<bool> HasSettlementEntryAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entries = await _ledgerRepository.GetForSourceAsync(RequestSourceType, requestId, cancellationToken);
        return entries.Any(entry => entry.EntryType == LedgerEntryType.RoadsideAssistanceRevenue);
    }
}
