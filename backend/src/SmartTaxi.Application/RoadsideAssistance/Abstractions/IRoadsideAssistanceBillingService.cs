using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>Mirrors IMaintenanceBillingService exactly — same Finance integration seam, same settlement-recovery contract, applied to roadside partner settlement instead of garage settlement.</summary>
public interface IRoadsideAssistanceBillingService
{
    Task<bool> TrySettleRequestAsync(
        Guid requestId, Guid requesterUserId, RoadsideRequesterRole requesterRole, Guid partnerUserId, decimal amount, string currency,
        DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Read-only recovery check: does the expected RoadsideAssistanceRevenue ledger entry for this request already exist? Never a second Finance abstraction — reuses the existing IFinancialLedgerRepository.GetForSourceAsync equivalent.</summary>
    Task<bool> HasSettlementEntryAsync(Guid requestId, CancellationToken cancellationToken);
}
