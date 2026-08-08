using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Application.Payments.Ledger;

/// <summary>
/// Concrete posting policy (lives in Application, like RevenueSharingCalculator,
/// since it composes Application-layer repositories rather than being a
/// swappable infrastructure concern).
///
/// Simplified single-line double-entry model: the Platform account is the
/// system's clearing house, since Customers are never given a ledger account
/// (only the eight actor types the master prompt specifies get one).
/// Posting-policy table for a payment confirmation (percentage-contract
/// Driver with an Owner; F=fare, C=commission, D=driver share, O=owner
/// share, F=C+D+O):
///   1. PaymentCollected   Debit=Platform  Credit=Platform  Credit.Pending   += F
///   2. PlatformCommission Debit=Platform  Credit=Platform  Debit.Pending   -= C, Credit.Available += C
///   3. DriverEarning      Debit=Platform  Credit=Driver    Debit.Pending   -= D, Credit.Available += D
///   4. OwnerEarning       Debit=Platform  Credit=Owner     Debit.Pending   -= O, Credit.Available += O
/// Platform.Pending nets to exactly 0 after all lines (F in, C+D+O out) —
/// the whole fare is fully distributed every time, by construction.
/// A Refund is a single entry: Debit=Platform Credit=Platform,
/// Debit.Available -= amount (absorbed from Platform's retained commission —
/// Driver/Owner earnings already paid are not automatically clawed back in
/// this simplified model, a documented limitation).
/// </summary>
public sealed class LedgerPostingService : ILedgerPostingService
{
    private const string PaymentSourceType = "Payment";
    private const string RefundSourceType = "Refund";

    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IFinancialLedgerRepository _ledgerRepository;

    public LedgerPostingService(IFinancialAccountRepository accountRepository, IFinancialLedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task PostPaymentConfirmedAsync(
        Guid paymentId, Guid driverUserId, Guid ownerUserId, decimal finalFareAmount, decimal platformCommissionAmount,
        decimal driverAmount, decimal ownerAmount, string currency, Guid? createdBy, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var platformAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, currency, cancellationToken);
        var driverAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Driver, driverUserId, currency, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            new(platformAccount.Id, platformAccount.Id, finalFareAmount, LedgerEntryType.PaymentCollected, "Encaissement du paiement",
                DebitEffects: [],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, finalFareAmount)]),

            new(platformAccount.Id, platformAccount.Id, platformCommissionAmount, LedgerEntryType.PlatformCommission, "Commission plateforme",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, -platformCommissionAmount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, platformCommissionAmount)])
        };

        if (driverAmount > 0)
        {
            lines.Add(new LedgerPostingLine(platformAccount.Id, driverAccount.Id, driverAmount, LedgerEntryType.DriverEarning, "Gain chauffeur",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, -driverAmount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, driverAmount)]));
        }

        if (ownerAmount > 0)
        {
            var ownerAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.TaxiOwner, ownerUserId, currency, cancellationToken);

            lines.Add(new LedgerPostingLine(platformAccount.Id, ownerAccount.Id, ownerAmount, LedgerEntryType.OwnerEarning, "Gain propriétaire",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Pending, -ownerAmount)],
                CreditEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, ownerAmount)]));
        }

        await _ledgerRepository.PostBatchAsync(PaymentSourceType, paymentId, currency, createdBy, utcNow, lines, cancellationToken);
    }

    public async Task PostRefundAsync(Guid refundRecordId, decimal amount, string currency, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var platformAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, currency, cancellationToken);

        var lines = new List<LedgerPostingLine>
        {
            new(platformAccount.Id, platformAccount.Id, amount, LedgerEntryType.Refund, "Remboursement client",
                DebitEffects: [new FinancialBalanceEffect(FinancialBalanceBucket.Available, -amount)],
                CreditEffects: [])
        };

        await _ledgerRepository.PostBatchAsync(RefundSourceType, refundRecordId, currency, createdBy, utcNow, lines, cancellationToken);
    }
}
