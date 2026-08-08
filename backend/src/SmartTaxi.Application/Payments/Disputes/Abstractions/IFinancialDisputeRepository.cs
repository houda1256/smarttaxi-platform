using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Payments.Disputes.Abstractions;

/// <summary>Release returns DisputedAmount to Available; Adjust removes it from Reserved permanently (absorbed — mirrors LedgerPostingService.PostRefundAsync's own Platform-absorption simplification).</summary>
public enum DisputeResolutionOutcome
{
    Release,
    Adjust
}

public interface IFinancialDisputeRepository
{
    Task<FinancialDispute?> GetByIdAsync(Guid disputeId, CancellationToken cancellationToken);

    Task<PagedResult<FinancialDispute>> GetRaisedByUserAsync(
        Guid raisedBy, FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<FinancialDispute>> GetForReviewAsync(
        FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the dispute and atomically moves DisputedAmount from Available
    /// to Reserved on the relevant FinancialAccount — the Payout's
    /// beneficiary account when RelatedPayoutId is set, otherwise the
    /// Platform account (mirrors PaymentCollected/Refund's own use of
    /// Platform as the counterparty, since Customers never get a ledger
    /// account). Returns false, with neither the dispute row nor the
    /// reservation committed, if that account's AvailableBalance can't cover
    /// DisputedAmount.
    /// </summary>
    Task<bool> TryOpenAsync(FinancialDispute dispute, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryStartReviewAsync(Guid disputeId, Guid assignedFinanceManagerId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded by Status == UnderReview. Atomically settles the reservation per DisputeResolutionOutcome alongside the status change.</summary>
    Task<bool> TryResolveAsync(
        Guid disputeId, string resolution, DisputeResolutionOutcome outcome, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded by Status == UnderReview. Always releases the reservation — a rejected dispute means the original transaction stands.</summary>
    Task<bool> TryRejectAsync(Guid disputeId, string resolution, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded by Status == UnderReview. No balance change — funds remain reserved while escalated for further review.</summary>
    Task<bool> TryEscalateAsync(Guid disputeId, DateTime utcNow, CancellationToken cancellationToken);
}
