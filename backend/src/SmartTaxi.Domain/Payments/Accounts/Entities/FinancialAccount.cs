using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Accounts.Enums;

namespace SmartTaxi.Domain.Payments.Accounts.Entities;

/// <summary>
/// One row per (AccountType, OwnerReferenceId) pair — exactly one Platform
/// account exists (OwnerReferenceId null); every other type is scoped to the
/// referenced actor (Driver/TaxiOwner/GaragePartner/RoadsideAssistancePartner/
/// Advertiser: their UserId; BusinessCustomer: the BusinessCustomer.Id;
/// CashRegister: the CashRegister.Id). Balances are only ever mutated by
/// atomic repository-level ledger-posting guards (see
/// IFinancialLedgerRepository.PostBatchAsync) — never a domain method — so a
/// balance can never change without a corresponding immutable
/// FinancialLedgerEntry, per the master prompt's own rule.
/// </summary>
public sealed class FinancialAccount : AggregateRoot
{
    public FinancialAccountType AccountType { get; private set; }
    public Guid? OwnerReferenceId { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public decimal PendingBalance { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal PaidOutBalance { get; private set; }
    public decimal DebtBalance { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private FinancialAccount()
    {
    }

    private FinancialAccount(FinancialAccountType accountType, Guid? ownerReferenceId, string currency, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        AccountType = accountType;
        OwnerReferenceId = ownerReferenceId;
        Currency = currency;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static FinancialAccount Open(FinancialAccountType accountType, Guid? ownerReferenceId, string currency, DateTime utcNow)
    {
        if (accountType == FinancialAccountType.Platform && ownerReferenceId is not null)
        {
            throw new ArgumentException("Le compte Platform ne référence aucun propriétaire spécifique.");
        }

        if (accountType != FinancialAccountType.Platform && ownerReferenceId is null)
        {
            throw new ArgumentException("Un compte non-Platform doit référencer un propriétaire.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new FinancialAccount(accountType, ownerReferenceId, currency.Trim().ToUpperInvariant(), utcNow);
    }

    /// <summary>Total funds not yet reserved, paid out, or owed — informational only, never persisted as its own column.</summary>
    public decimal NetBalance => PendingBalance + AvailableBalance - ReservedBalance - DebtBalance;
}
