namespace SmartTaxi.Application.Payments.Ledger.Abstractions;

/// <summary>
/// The posting-policy layer: translates a financial event (payment
/// confirmed, refund issued, payout completed, cash settled, manual
/// adjustment) into the specific set of balanced FinancialLedgerEntry rows
/// and account-balance effects it produces, then posts them as one atomic
/// batch via IFinancialLedgerRepository. See the Phase 5B final report for
/// the complete posting-policy table.
/// </summary>
public interface ILedgerPostingService
{
    /// <summary>Posts PaymentCollected + PlatformCommission + (DriverEarning) + (OwnerEarning, when ownerAmount &gt; 0 — skipped for independent drivers) as one batch, keyed by (SourceType="Payment", SourceId=paymentId) for idempotency.</summary>
    Task PostPaymentConfirmedAsync(
        Guid paymentId, Guid driverUserId, Guid ownerUserId, decimal finalFareAmount, decimal platformCommissionAmount,
        decimal driverAmount, decimal ownerAmount, string currency, Guid? createdBy, DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>Posts a single Refund entry keyed by (SourceType="Refund", SourceId=refundRecordId) — Platform absorbs the refund from its own retained commission (a documented simplification; Driver/Owner earnings already credited are not automatically clawed back).</summary>
    Task PostRefundAsync(Guid refundRecordId, decimal amount, string currency, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken);
}
