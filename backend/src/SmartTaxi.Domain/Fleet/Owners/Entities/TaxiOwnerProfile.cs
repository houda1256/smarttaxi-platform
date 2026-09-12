using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Owners.Enums;
using SmartTaxi.Domain.Fleet.Owners.Events;
using SmartTaxi.Domain.Fleet.Owners.ValueObjects;

namespace SmartTaxi.Domain.Fleet.Owners.Entities;

/// <summary>
/// Single flat entity for both owner types (discriminated by OwnerType),
/// matching this codebase's existing style rather than introducing EF
/// inheritance mapping for the first time. Status transitions (verification,
/// account status) are enforced as atomic repository-level guards, not
/// domain methods — same pattern as every prior sub-slice.
/// </summary>
public sealed class TaxiOwnerProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public OwnerType OwnerType { get; private set; }

    // Individual-only fields.
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? NationalId { get; private set; }

    // Company-only fields.
    public string? LegalName { get; private set; }
    public string? TradeName { get; private set; }
    public string? TaxIdentifier { get; private set; }
    public string? RegistrationNumber { get; private set; }

    public Address Address { get; private set; } = null!;
    public BankInformation BankInformation { get; private set; } = null!;

    public OwnerVerificationStatus VerificationStatus { get; private set; }
    public OwnerAccountStatus AccountStatus { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaxiOwnerProfile()
    {
    }

    private TaxiOwnerProfile(Guid userId, OwnerType ownerType, Address address, BankInformation bankInformation, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        OwnerType = ownerType;
        Address = address;
        BankInformation = bankInformation;
        VerificationStatus = OwnerVerificationStatus.Pending;
        AccountStatus = OwnerAccountStatus.Active;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new TaxiOwnerProfileCreated(Id, utcNow));
    }

    public static TaxiOwnerProfile CreateIndividual(
        Guid userId, string firstName, string lastName, string nationalId,
        Address address, BankInformation bankInformation, DateTime utcNow)
    {
        var profile = new TaxiOwnerProfile(userId, OwnerType.Individual, address, bankInformation, utcNow);
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.NationalId = nationalId;
        return profile;
    }

    public static TaxiOwnerProfile CreateCompany(
        Guid userId, string legalName, string tradeName, string taxIdentifier, string registrationNumber,
        Address address, BankInformation bankInformation, DateTime utcNow)
    {
        var profile = new TaxiOwnerProfile(userId, OwnerType.Company, address, bankInformation, utcNow);
        profile.LegalName = legalName;
        profile.TradeName = tradeName;
        profile.TaxIdentifier = taxIdentifier;
        profile.RegistrationNumber = registrationNumber;
        return profile;
    }

    public void UpdateIndividualDetails(string firstName, string lastName, string nationalId, DateTime utcNow)
    {
        if (OwnerType != OwnerType.Individual)
        {
            throw new InvalidOperationException("Ce profil n'est pas un propriétaire individuel.");
        }

        FirstName = firstName;
        LastName = lastName;
        NationalId = nationalId;
        UpdatedAt = utcNow;
    }

    public void UpdateCompanyDetails(
        string legalName, string tradeName, string taxIdentifier, string registrationNumber, DateTime utcNow)
    {
        if (OwnerType != OwnerType.Company)
        {
            throw new InvalidOperationException("Ce profil n'est pas un propriétaire entreprise.");
        }

        LegalName = legalName;
        TradeName = tradeName;
        TaxIdentifier = taxIdentifier;
        RegistrationNumber = registrationNumber;
        UpdatedAt = utcNow;
    }

    public void UpdateContactInformation(Address address, BankInformation bankInformation, DateTime utcNow)
    {
        Address = address;
        BankInformation = bankInformation;
        UpdatedAt = utcNow;
    }
}
