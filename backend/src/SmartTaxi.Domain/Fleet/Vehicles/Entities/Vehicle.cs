using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Events;

namespace SmartTaxi.Domain.Fleet.Vehicles.Entities;

/// <summary>
/// OwnerId always references the owning User (a TaxiOwner's UserId, or an
/// independent Driver's own UserId) — FleetId is null when the vehicle isn't
/// attached to any fleet organization (e.g. an independent driver's own
/// single vehicle). Status transitions (verification, suspension,
/// retirement) are enforced as atomic repository-level guards, not domain
/// methods — same pattern as every prior sub-slice. Approve/Reject move both
/// VerificationStatus and OperationalStatus together in one atomic update;
/// day-to-day OperationalStatus moves (maintenance/suspend/retire) never
/// touch VerificationStatus once granted.
/// </summary>
public sealed class Vehicle : AggregateRoot
{
    public Guid OwnerId { get; private set; }
    public Guid? FleetId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public string LicensePlate { get; private set; } = string.Empty;
    public string? Vin { get; private set; }
    public int CurrentMileage { get; private set; }
    public FuelType FuelType { get; private set; }
    public TransmissionType TransmissionType { get; private set; }
    public int SeatCount { get; private set; }
    public bool HasAirConditioning { get; private set; }
    public bool IsAccessible { get; private set; }
    public VehicleCategory VehicleCategory { get; private set; }
    public VehicleOperationalStatus OperationalStatus { get; private set; }
    public VehicleVerificationStatus VerificationStatus { get; private set; }
    public string? MainPhotoReference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Vehicle()
    {
    }

    private Vehicle(
        Guid ownerId, Guid? fleetId, string brand, string model, int year, string color, string licensePlate,
        string? vin, int currentMileage, FuelType fuelType, TransmissionType transmissionType, int seatCount,
        bool hasAirConditioning, bool isAccessible, VehicleCategory vehicleCategory, string? mainPhotoReference,
        DateTime utcNow)
        : base(Guid.NewGuid())
    {
        OwnerId = ownerId;
        FleetId = fleetId;
        Brand = brand;
        Model = model;
        Year = year;
        Color = color;
        LicensePlate = licensePlate;
        Vin = vin;
        CurrentMileage = currentMileage;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        SeatCount = seatCount;
        HasAirConditioning = hasAirConditioning;
        IsAccessible = isAccessible;
        VehicleCategory = vehicleCategory;
        MainPhotoReference = mainPhotoReference;
        OperationalStatus = VehicleOperationalStatus.PendingVerification;
        VerificationStatus = VehicleVerificationStatus.Pending;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new VehicleRegistered(Id, ownerId, utcNow));
    }

    public static Vehicle Register(
        Guid ownerId, Guid? fleetId, string brand, string model, int year, string color, string licensePlate,
        string? vin, int currentMileage, FuelType fuelType, TransmissionType transmissionType, int seatCount,
        bool hasAirConditioning, bool isAccessible, VehicleCategory vehicleCategory, string? mainPhotoReference,
        DateTime utcNow) =>
        new(ownerId, fleetId, brand, model, year, color, licensePlate, vin, currentMileage, fuelType,
            transmissionType, seatCount, hasAirConditioning, isAccessible, vehicleCategory, mainPhotoReference, utcNow);

    public void UpdateDetails(
        string brand, string model, int year, string color, string licensePlate, string? vin, int currentMileage,
        FuelType fuelType, TransmissionType transmissionType, int seatCount, bool hasAirConditioning,
        bool isAccessible, VehicleCategory vehicleCategory, string? mainPhotoReference, DateTime utcNow)
    {
        Brand = brand;
        Model = model;
        Year = year;
        Color = color;
        LicensePlate = licensePlate;
        Vin = vin;
        CurrentMileage = currentMileage;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        SeatCount = seatCount;
        HasAirConditioning = hasAirConditioning;
        IsAccessible = isAccessible;
        VehicleCategory = vehicleCategory;
        MainPhotoReference = mainPhotoReference;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// True only when the vehicle has passed platform verification and its
    /// current operational status is Active — a non-approved or
    /// under-maintenance/suspended/retired vehicle is never eligible.
    /// Critical-document expiration is layered on top of this at the query
    /// level (Application), since that requires joining VehicleDocument.
    /// </summary>
    public bool IsCurrentlyEligibleForRides() =>
        VerificationStatus == VehicleVerificationStatus.Approved && OperationalStatus == VehicleOperationalStatus.Active;
}
