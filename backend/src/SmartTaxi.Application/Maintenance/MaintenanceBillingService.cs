using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Application.Maintenance;

/// <summary>
/// Concrete Finance integration (lives in Application, like
/// LedgerPostingService/AdvertisingBillingService, since it only composes
/// Application-layer Payments repositories — no Infrastructure dependency).
/// Single-line posting: Debit=TaxiOwner (Debt bucket, they now owe this
/// amount), Credit=GaragePartner (Available bucket, revenue recognized) — no
/// platform commission (approved MVP design), the full settled amount goes to
/// the garage. Keyed by (SourceType="MaintenanceRequest",
/// SourceId=requestId, EntryType=MaintenanceRevenue) so a request can only
/// ever be settled once through this seam.
/// </summary>
public sealed class MaintenanceBillingService : IMaintenanceBillingService
{
    private const string RequestSourceType = "MaintenanceRequest";

    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IFinancialLedgerRepository _ledgerRepository;

    public MaintenanceBillingService(IFinancialAccountRepository accountRepository, IFinancialLedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<bool> TrySettleRequestAsync(
        Guid requestId, Guid ownerUserId, Guid garageUserId, decimal amount, string currency, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return false;
        }

        var ownerAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerUserId, currency, cancellationToken);
        var garageAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.GaragePartner, garageUserId, currency, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            new(ownerAccount.Id, garageAccount.Id, amount, LedgerEntryType.MaintenanceRevenue, "Règlement intervention garage",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Debt, amount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, amount)])
        };

        return await _ledgerRepository.PostBatchAsync(RequestSourceType, requestId, currency, null, utcNow, lines, cancellationToken);
    }

    public async Task<bool> HasSettlementEntryAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var entries = await _ledgerRepository.GetForSourceAsync(RequestSourceType, requestId, cancellationToken);
        return entries.Any(entry => entry.EntryType == LedgerEntryType.MaintenanceRevenue);
    }
}
