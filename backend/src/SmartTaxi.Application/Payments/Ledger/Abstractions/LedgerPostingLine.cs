using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Application.Payments.Ledger.Abstractions;

/// <summary>
/// One immutable ledger entry to post, plus the specific balance-bucket
/// effects it produces on each side. DebitAccountId/CreditAccountId may be
/// the same account (see FinancialLedgerEntry's own doc comment) — a single
/// entry can then legitimately carry effects on both sides of that one
/// account (e.g. moving value from its Pending bucket to its Available
/// bucket). Effects lists may be empty when a side has no balance impact
/// (e.g. the Platform side of a Payout, which is a system-boundary
/// placeholder rather than a real counterparty).
/// </summary>
public sealed record LedgerPostingLine(
    Guid DebitAccountId,
    Guid CreditAccountId,
    decimal Amount,
    LedgerEntryType EntryType,
    string? Description,
    IReadOnlyCollection<FinancialBalanceEffect> DebitEffects,
    IReadOnlyCollection<FinancialBalanceEffect> CreditEffects);
