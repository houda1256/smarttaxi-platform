using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Abstractions;

/// <summary>Outcome of a Processing -> Paid completion attempt, distinguishing "lost the atomic race" from "beneficiary balance can no longer cover it" so the handler can react appropriately to each.</summary>
public enum PayoutCompletionOutcome
{
    Completed,
    NotInProcessingState,
    InsufficientAvailableBalance
}

public interface IPayoutRepository
{
    Task AddAsync(Payout payout, CancellationToken cancellationToken);

    Task<Payout?> GetByIdAsync(Guid payoutId, CancellationToken cancellationToken);

    Task<PagedResult<Payout>> GetForBeneficiaryAccountAsync(
        Guid beneficiaryAccountId, PayoutStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<Payout>> GetForAdminAsync(
        FinancialAccountType? beneficiaryType, PayoutStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> TrySubmitForApprovalAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid payoutId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryStartProcessingAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryMarkFailedAsync(Guid payoutId, string reason, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// The sole place a Payout's money actually moves: atomically (single DB
    /// transaction in the real implementation) re-verifies the Payout is
    /// still Processing AND the beneficiary's FinancialAccount still has
    /// enough AvailableBalance, moves Available -> PaidOut on that account,
    /// transitions the Payout to Paid, and records the traceable
    /// FinancialLedgerEntry — all three together or none of them.
    /// </summary>
    Task<PayoutCompletionOutcome> TryCompleteAsync(Guid payoutId, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken);
}
