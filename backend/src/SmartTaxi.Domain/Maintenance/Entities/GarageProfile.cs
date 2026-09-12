using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Maintenance.Events;

namespace SmartTaxi.Domain.Maintenance.Entities;

/// <summary>
/// One profile per GaragePartner UserId (enforced by a DB unique index) — the
/// Identity/professional-account approval and GaragePartner role membership
/// are Identity's own responsibility (ProfessionalAccountRequest,
/// UserRole.GaragePartner); this entity only ever adds the business metadata
/// Identity does not model, the exact same "thin, UserId-keyed aggregate"
/// shape as AdvertiserProfile/LoyaltyAccount. No VerificationStatus field
/// here — that state already lives entirely in Identity.
/// </summary>
public sealed class GarageProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string Address { get; private set; } = string.Empty;

    /// <summary>Free-text, normalized (trimmed/uppercased) — no controlled Zone/Geography catalog exists in this codebase (same documented interim limitation as Advertising's TargetCity), not a real zone.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Comma-separated Fleet.Vehicles.Enums.VehicleCategory names — a Domain enum referenced directly (cheap/stable), never a duplicated category catalog.</summary>
    public string? SupportedVehicleCategories { get; private set; }

    /// <summary>Free-text service list — no service catalog (out of scope).</summary>
    public string? AvailableServices { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private GarageProfile()
    {
    }

    private GarageProfile(
        Guid userId, string businessName, string? legalName, string address, string city, string? supportedVehicleCategories,
        string? availableServices, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        BusinessName = businessName;
        LegalName = legalName;
        Address = address;
        City = city;
        SupportedVehicleCategories = supportedVehicleCategories;
        AvailableServices = availableServices;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new GarageProfileRegistered(Id, userId, utcNow));
    }

    public static GarageProfile Register(
        Guid userId, string businessName, string? legalName, string address, string city, string? supportedVehicleCategories,
        string? availableServices, DateTime utcNow)
    {
        ValidateFields(businessName, address, city);

        return new GarageProfile(
            userId, businessName.Trim(), legalName?.Trim(), address.Trim(), NormalizeCity(city)!, supportedVehicleCategories?.Trim(),
            availableServices?.Trim(), utcNow);
    }

    public void UpdateProfile(
        string businessName, string? legalName, string address, string city, string? supportedVehicleCategories,
        string? availableServices, DateTime utcNow)
    {
        ValidateFields(businessName, address, city);

        BusinessName = businessName.Trim();
        LegalName = legalName?.Trim();
        Address = address.Trim();
        City = NormalizeCity(city)!;
        SupportedVehicleCategories = supportedVehicleCategories?.Trim();
        AvailableServices = availableServices?.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateFields(string businessName, string address, string city)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException("Le nom commercial est requis.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("L'adresse est requise.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("La ville est requise.");
        }
    }

    private static string? NormalizeCity(string? city) => string.IsNullOrWhiteSpace(city) ? null : city.Trim().ToUpperInvariant();
}
