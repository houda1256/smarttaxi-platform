using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

/// <summary>
/// CurrentCreditUsage is only ever mutated by atomic repository-level guards
/// (incremented when a deferred-payment GroupedInvoice/Ride is billed to this
/// account, decremented when it's paid) — the credit-limit check itself
/// ("cannot exceed CreditLimit") is enforced atomically at that same
/// increment point, never as a separate non-atomic read-then-write.
/// </summary>
public sealed class BusinessCustomer : AggregateRoot
{
    public string LegalName { get; private set; } = string.Empty;
    public string TaxIdentifier { get; private set; } = string.Empty;
    public string BillingAddress { get; private set; } = string.Empty;
    public string ContactPersonName { get; private set; } = string.Empty;
    public string ContactPersonEmail { get; private set; } = string.Empty;
    public string? ContactPersonPhone { get; private set; }
    public DeferredPaymentTerm PaymentTerms { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal CurrentCreditUsage { get; private set; }
    public BusinessCustomerStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BusinessCustomer()
    {
    }

    private BusinessCustomer(
        string legalName, string taxIdentifier, string billingAddress, string contactPersonName, string contactPersonEmail,
        string? contactPersonPhone, DeferredPaymentTerm paymentTerms, decimal creditLimit, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        LegalName = legalName;
        TaxIdentifier = taxIdentifier;
        BillingAddress = billingAddress;
        ContactPersonName = contactPersonName;
        ContactPersonEmail = contactPersonEmail;
        ContactPersonPhone = contactPersonPhone;
        PaymentTerms = paymentTerms;
        CreditLimit = creditLimit;
        CurrentCreditUsage = 0m;
        Status = BusinessCustomerStatus.Active;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static BusinessCustomer Register(
        string legalName, string taxIdentifier, string billingAddress, string contactPersonName, string contactPersonEmail,
        string? contactPersonPhone, DeferredPaymentTerm paymentTerms, decimal creditLimit, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException("La raison sociale est requise.");
        }

        if (string.IsNullOrWhiteSpace(taxIdentifier))
        {
            throw new ArgumentException("L'identifiant fiscal est requis.");
        }

        if (creditLimit < 0)
        {
            throw new ArgumentException("La limite de crédit ne peut pas être négative.");
        }

        return new BusinessCustomer(
            legalName, taxIdentifier, billingAddress, contactPersonName, contactPersonEmail, contactPersonPhone,
            paymentTerms, creditLimit, utcNow);
    }

    public decimal RemainingCredit => CreditLimit - CurrentCreditUsage;
}
