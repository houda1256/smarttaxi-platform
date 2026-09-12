namespace SmartTaxi.Application.Maintenance.Abstractions;

/// <summary>Mirrors IAdvertisingBillingService exactly — same Finance integration seam, same settlement-recovery contract, applied to garage settlement instead of advertising campaigns.</summary>
public interface IMaintenanceBillingService
{
    Task<bool> TrySettleRequestAsync(
        Guid requestId, Guid ownerUserId, Guid garageUserId, decimal amount, string currency, DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>Read-only recovery check: does the expected MaintenanceRevenue ledger entry for this request already exist? Never a second Finance abstraction — reuses the existing IFinancialLedgerRepository.GetForSourceAsync.</summary>
    Task<bool> HasSettlementEntryAsync(Guid requestId, CancellationToken cancellationToken);
}
