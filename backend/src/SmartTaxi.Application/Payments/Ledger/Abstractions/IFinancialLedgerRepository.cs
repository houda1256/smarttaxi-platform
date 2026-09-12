using SmartTaxi.Domain.Payments.Ledger.Entities;

namespace SmartTaxi.Application.Payments.Ledger.Abstractions;

public interface IFinancialLedgerRepository
{
    /// <summary>
    /// Posts every line in one atomic transaction, applying each line's debit-
    /// and credit-side balance effects alongside the immutable entry rows
    /// themselves — a balance can never be observed to have moved without its
    /// corresponding ledger entry already committed, and vice versa. Guarded
    /// by a DB unique index on (SourceType, SourceId, EntryType): if any line
    /// in the batch was already posted for this exact source, the whole call
    /// is a no-op and returns false (the "duplicate posting must be prevented
    /// through idempotency" requirement) — callers should treat false as "did
    /// nothing, which is fine" rather than an error, mirroring the outer
    /// operation's own idempotency (e.g. ConfirmPayment).
    /// </summary>
    Task<bool> PostBatchAsync(
        string sourceType, Guid sourceId, string currency, Guid? createdBy, DateTime utcNow,
        IReadOnlyCollection<LedgerPostingLine> lines, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForAccountAsync(
        Guid accountId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);

    Task<bool> HasEntriesForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken);
}
