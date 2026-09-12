using SmartTaxi.Domain.Payments.Ledger.Enums;

namespace SmartTaxi.Domain.Payments.Ledger.Entities;

/// <summary>
/// Immutable, append-only — the platform's single source of financial truth.
/// Never updated, never deleted; a correction is always a new entry with
/// <see cref="ReversalOfEntryId"/> pointing back at the entry it reverses.
/// DebitAccountId/CreditAccountId are allowed to reference the same account
/// (e.g. moving value between two of that account's own balance buckets, or
/// recognizing revenue that has no internal counterparty account to model —
/// Customers are never given a ledger account) — this is a deliberate,
/// documented simplification of a full formal double-entry ledger (see the
/// Phase 5B final report for the complete posting-policy table), not an
/// error. "Debits and credits remain balanced" is satisfied because a single
/// entry's Amount is definitionally both its debit and credit value.
/// </summary>
public sealed class FinancialLedgerEntry
{
    public Guid Id { get; private set; }
    public string TransactionNumber { get; private set; } = string.Empty;
    public Guid DebitAccountId { get; private set; }
    public Guid CreditAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public LedgerEntryType EntryType { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? ReversalOfEntryId { get; private set; }

    private FinancialLedgerEntry()
    {
    }

    private FinancialLedgerEntry(
        Guid debitAccountId, Guid creditAccountId, decimal amount, string currency, LedgerEntryType entryType,
        string sourceType, Guid sourceId, string? description, Guid? createdBy, Guid? reversalOfEntryId, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        TransactionNumber = GenerateTransactionNumber(utcNow);
        DebitAccountId = debitAccountId;
        CreditAccountId = creditAccountId;
        Amount = amount;
        Currency = currency;
        EntryType = entryType;
        SourceType = sourceType;
        SourceId = sourceId;
        Description = description;
        CreatedAt = utcNow;
        CreatedBy = createdBy;
        ReversalOfEntryId = reversalOfEntryId;
    }

    public static FinancialLedgerEntry Post(
        Guid debitAccountId, Guid creditAccountId, decimal amount, string currency, LedgerEntryType entryType,
        string sourceType, Guid sourceId, string? description, Guid? createdBy, DateTime utcNow, Guid? reversalOfEntryId = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Le montant d'une écriture comptable doit être positif.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("Le type de source est requis pour la traçabilité.");
        }

        return new FinancialLedgerEntry(
            debitAccountId, creditAccountId, amount, currency.Trim().ToUpperInvariant(), entryType, sourceType, sourceId,
            description, createdBy, reversalOfEntryId, utcNow);
    }

    private static string GenerateTransactionNumber(DateTime utcNow) =>
        $"TXN-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
