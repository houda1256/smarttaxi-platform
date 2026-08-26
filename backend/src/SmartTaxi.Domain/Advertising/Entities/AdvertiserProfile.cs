using SmartTaxi.Domain.Advertising.Events;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>
/// One profile per Advertiser UserId (enforced by a DB unique index) — the
/// Identity/professional-account approval and Advertiser role membership are
/// Identity's own responsibility (ProfessionalAccountRequest,
/// UserRole.Advertiser); this entity only ever adds the business/billing
/// metadata Identity does not model, exactly the same "thin, UserId-keyed
/// aggregate" shape as Loyalty's LoyaltyAccount. Agency/multi-advertiser
/// management is explicitly deferred (Module 8 audit scope decision).
/// </summary>
public sealed class AdvertiserProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string TaxIdentifier { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private AdvertiserProfile()
    {
    }

    private AdvertiserProfile(
        Guid userId, string businessName, string legalName, string taxIdentifier, string city, string address,
        string contactEmail, string? contactPhone, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        BusinessName = businessName;
        LegalName = legalName;
        TaxIdentifier = taxIdentifier;
        City = city;
        Address = address;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new AdvertiserProfileRegistered(Id, userId, utcNow));
    }

    public static AdvertiserProfile Register(
        Guid userId, string businessName, string legalName, string taxIdentifier, string city, string address,
        string contactEmail, string? contactPhone, DateTime utcNow)
    {
        ValidateFields(businessName, legalName, taxIdentifier, city, contactEmail);

        return new AdvertiserProfile(
            userId, businessName.Trim(), legalName.Trim(), taxIdentifier.Trim(), city.Trim(), (address ?? string.Empty).Trim(),
            contactEmail.Trim(), contactPhone?.Trim(), utcNow);
    }

    public void UpdateProfile(
        string businessName, string legalName, string taxIdentifier, string city, string address, string contactEmail,
        string? contactPhone, DateTime utcNow)
    {
        ValidateFields(businessName, legalName, taxIdentifier, city, contactEmail);

        BusinessName = businessName.Trim();
        LegalName = legalName.Trim();
        TaxIdentifier = taxIdentifier.Trim();
        City = city.Trim();
        Address = (address ?? string.Empty).Trim();
        ContactEmail = contactEmail.Trim();
        ContactPhone = contactPhone?.Trim();
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateFields(string businessName, string legalName, string taxIdentifier, string city, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException("Le nom commercial est requis.");
        }

        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException("La raison sociale est requise.");
        }

        if (string.IsNullOrWhiteSpace(taxIdentifier))
        {
            throw new ArgumentException("L'identifiant fiscal est requis.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("La ville est requise.");
        }

        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new ArgumentException("L'email de contact est requis.");
        }
    }
}
