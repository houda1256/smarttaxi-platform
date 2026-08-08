using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Domain.Payments.Payouts.Entities;

/// <summary>
/// Status transitions (approve/process/complete/fail/reject/cancel) are
/// atomic repository-level guards, never domain mutation methods — same
/// convention as every other financial status machine in this module. The
/// AvailableBalance check against the beneficiary's FinancialAccount happens
/// in the Application layer before a Payout is even requested, and again
/// atomically when it moves to Paid, so a Payout can never exceed what the
/// beneficiary actually has available.
/// </summary>
public sealed class Payout : AggregateRoot
{
    public Guid BeneficiaryAccountId { get; private set; }
    public FinancialAccountType BeneficiaryType { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PayoutMethod Method { get; private set; }
    public PayoutFrequency Frequency { get; private set; }
    public PayoutStatus Status { get; private set; }

    public DateTime RequestedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Payout()
    {
    }

    private Payout(
        Guid beneficiaryAccountId, FinancialAccountType beneficiaryType, decimal amount, string currency,
        PayoutMethod method, PayoutFrequency frequency, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        BeneficiaryAccountId = beneficiaryAccountId;
        BeneficiaryType = beneficiaryType;
        Amount = amount;
        Currency = currency;
        Method = method;
        Frequency = frequency;
        Status = PayoutStatus.Requested;
        RequestedAt = utcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static Payout Request(
        Guid beneficiaryAccountId, FinancialAccountType beneficiaryType, decimal amount, string currency,
        PayoutMethod method, PayoutFrequency frequency, DateTime utcNow)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Le montant du versement doit être positif.");
        }

        if (beneficiaryType is not (FinancialAccountType.Driver or FinancialAccountType.TaxiOwner
            or FinancialAccountType.GaragePartner or FinancialAccountType.RoadsideAssistancePartner))
        {
            throw new ArgumentException("Seuls les chauffeurs, propriétaires et partenaires peuvent recevoir un versement.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new Payout(beneficiaryAccountId, beneficiaryType, amount, currency.Trim().ToUpperInvariant(), method, frequency, utcNow);
    }
}
