using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.RoadsideAssistance.Events;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.RoadsideAssistance.Entities;

/// <summary>
/// One profile per RoadsideAssistancePartner UserId (enforced by a DB unique
/// index) — the Identity/professional-account approval and
/// RoadsideAssistancePartner role membership are Identity's own
/// responsibility (ProfessionalAccountRequest, UserRole.RoadsideAssistancePartner);
/// this entity only ever adds the business metadata Identity does not model,
/// the exact same "thin, UserId-keyed aggregate" shape as GarageProfile/
/// AdvertiserProfile. No VerificationStatus field here — that state already
/// lives entirely in Identity. Latitude/Longitude are additive relative to
/// GarageProfile's own shape — needed so distance-based recommendation
/// (see the recommendation query) has a real partner base location to
/// compute from, since no such field exists on GarageProfile.
/// </summary>
public sealed class RoadsidePartnerProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string Address { get; private set; } = string.Empty;

    /// <summary>Free-text, normalized (trimmed/uppercased) — no controlled Zone/Geography catalog exists in this codebase (same documented interim limitation as Advertising/Maintenance), not a real zone.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Comma-separated RoadsideAssistance.Enums.RoadsideServiceType names — no service catalog (out of scope), same convention as GarageProfile.AvailableServices.</summary>
    public string? SupportedServiceTypes { get; private set; }

    /// <summary>Comma-separated Fleet.Vehicles.Enums.VehicleCategory names — a Domain enum referenced directly (cheap/stable), never a duplicated category catalog, same convention as GarageProfile.</summary>
    public string? SupportedVehicleCategories { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private RoadsidePartnerProfile()
    {
    }

    private RoadsidePartnerProfile(
        Guid userId, string businessName, string? legalName, string address, string city, string? supportedServiceTypes,
        string? supportedVehicleCategories, double? latitude, double? longitude, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        BusinessName = businessName;
        LegalName = legalName;
        Address = address;
        City = city;
        SupportedServiceTypes = supportedServiceTypes;
        SupportedVehicleCategories = supportedVehicleCategories;
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new RoadsidePartnerProfileRegistered(Id, userId, utcNow));
    }

    public static RoadsidePartnerProfile Register(
        Guid userId, string businessName, string? legalName, string address, string city, string? supportedServiceTypes,
        string? supportedVehicleCategories, double? latitude, double? longitude, DateTime utcNow)
    {
        ValidateFields(businessName, address, city);
        ValidateOptionalCoordinate(latitude, longitude);

        return new RoadsidePartnerProfile(
            userId, businessName.Trim(), legalName?.Trim(), address.Trim(), NormalizeCity(city)!, supportedServiceTypes?.Trim(),
            supportedVehicleCategories?.Trim(), latitude, longitude, utcNow);
    }

    public void UpdateProfile(
        string businessName, string? legalName, string address, string city, string? supportedServiceTypes,
        string? supportedVehicleCategories, double? latitude, double? longitude, DateTime utcNow)
    {
        ValidateFields(businessName, address, city);
        ValidateOptionalCoordinate(latitude, longitude);

        BusinessName = businessName.Trim();
        LegalName = legalName?.Trim();
        Address = address.Trim();
        City = NormalizeCity(city)!;
        SupportedServiceTypes = supportedServiceTypes?.Trim();
        SupportedVehicleCategories = supportedVehicleCategories?.Trim();
        Latitude = latitude;
        Longitude = longitude;
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

    /// <summary>Reuses GeoCoordinate's own validated range check rather than duplicating it — both-or-neither, never a half-set coordinate pair.</summary>
    private static void ValidateOptionalCoordinate(double? latitude, double? longitude)
    {
        if (latitude is null && longitude is null)
        {
            return;
        }

        if (latitude is null || longitude is null)
        {
            throw new ArgumentException("La latitude et la longitude doivent être fournies ensemble.");
        }

        GeoCoordinate.Create(latitude.Value, longitude.Value);
    }

    private static string? NormalizeCity(string? city) => string.IsNullOrWhiteSpace(city) ? null : city.Trim().ToUpperInvariant();
}
