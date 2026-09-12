using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Events;

namespace SmartTaxi.Domain.Fleet.Drivers.Entities;

/// <summary>
/// VerificationStatus transitions (approve/reject/suspend) are enforced as
/// atomic repository-level guards, same as every prior sub-slice — but
/// SetAvailability is a genuine domain method: it's a single-owner action
/// (the driver sets their own status) with no reviewer-vs-owner race, so it
/// goes through the normal load-mutate-save path and actually raises its event.
/// </summary>
public sealed class DriverProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string DriverLicenseNumber { get; private set; } = string.Empty;
    public DateTime DriverLicenseExpiration { get; private set; }
    public string? TaxiLicenseNumber { get; private set; }
    public DriverVerificationStatus VerificationStatus { get; private set; }
    public DriverAvailabilityStatus AvailabilityStatus { get; private set; }
    public bool IndependentDriver { get; private set; }
    public Guid? CurrentVehicleId { get; private set; }
    public decimal AverageRating { get; private set; }
    public int CompletedRideCount { get; private set; }
    public int CancellationCount { get; private set; }
    public double? LastKnownLatitude { get; private set; }
    public double? LastKnownLongitude { get; private set; }
    public DateTime? LastLocationRecordedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DriverProfile()
    {
    }

    private DriverProfile(
        Guid userId, string driverLicenseNumber, DateTime driverLicenseExpiration, string? taxiLicenseNumber,
        bool independentDriver, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        DriverLicenseNumber = driverLicenseNumber;
        DriverLicenseExpiration = driverLicenseExpiration;
        TaxiLicenseNumber = taxiLicenseNumber;
        IndependentDriver = independentDriver;
        VerificationStatus = DriverVerificationStatus.PendingReview;
        AvailabilityStatus = DriverAvailabilityStatus.Offline;
        AverageRating = 0;
        CompletedRideCount = 0;
        CancellationCount = 0;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new DriverProfileCreated(Id, userId, utcNow));
    }

    public static DriverProfile Create(
        Guid userId, string driverLicenseNumber, DateTime driverLicenseExpiration, string? taxiLicenseNumber,
        bool independentDriver, DateTime utcNow) =>
        new(userId, driverLicenseNumber, driverLicenseExpiration, taxiLicenseNumber, independentDriver, utcNow);

    public void UpdateProfile(
        string driverLicenseNumber, DateTime driverLicenseExpiration, string? taxiLicenseNumber, DateTime utcNow)
    {
        DriverLicenseNumber = driverLicenseNumber;
        DriverLicenseExpiration = driverLicenseExpiration;
        TaxiLicenseNumber = taxiLicenseNumber;
        UpdatedAt = utcNow;
    }

    /// <summary>A driver cannot become operational (anything but Offline) before professional approval.</summary>
    public void SetAvailability(DriverAvailabilityStatus status, DateTime utcNow)
    {
        if (VerificationStatus != DriverVerificationStatus.Approved && status != DriverAvailabilityStatus.Offline)
        {
            throw new InvalidOperationException("Le chauffeur doit être approuvé avant de devenir opérationnel.");
        }

        AvailabilityStatus = status;
        UpdatedAt = utcNow;
        RaiseDomainEvent(new DriverAvailabilityChanged(Id, status, utcNow));
    }

    public void AssignVehicle(Guid vehicleId, DateTime utcNow)
    {
        CurrentVehicleId = vehicleId;
        UpdatedAt = utcNow;
    }

    public void UnassignVehicle(DateTime utcNow)
    {
        CurrentVehicleId = null;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// A lightweight position cache so the Ride module can search/rank
    /// available Drivers without an active Ride to attach telemetry to.
    /// Single-actor field (only the Driver's own device writes it) — no
    /// atomic guard needed, same as UpdateProfile.
    /// </summary>
    public void UpdateLastKnownLocation(double latitude, double longitude, DateTime utcNow)
    {
        LastKnownLatitude = latitude;
        LastKnownLongitude = longitude;
        LastLocationRecordedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
