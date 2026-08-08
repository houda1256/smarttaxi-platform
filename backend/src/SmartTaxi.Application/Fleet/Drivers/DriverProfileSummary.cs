using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Fleet.Drivers;

public sealed record DriverProfileSummary(
    Guid Id,
    Guid UserId,
    string DriverLicenseNumber,
    DateTime DriverLicenseExpiration,
    string? TaxiLicenseNumber,
    DriverVerificationStatus VerificationStatus,
    DriverAvailabilityStatus AvailabilityStatus,
    bool IndependentDriver,
    Guid? CurrentVehicleId,
    decimal AverageRating,
    int CompletedRideCount,
    int CancellationCount,
    double? LastKnownLatitude,
    double? LastKnownLongitude,
    DateTime? LastLocationRecordedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static DriverProfileSummary FromEntity(DriverProfile profile) => new(
        profile.Id, profile.UserId, profile.DriverLicenseNumber, profile.DriverLicenseExpiration,
        profile.TaxiLicenseNumber, profile.VerificationStatus, profile.AvailabilityStatus, profile.IndependentDriver,
        profile.CurrentVehicleId, profile.AverageRating, profile.CompletedRideCount, profile.CancellationCount,
        profile.LastKnownLatitude, profile.LastKnownLongitude, profile.LastLocationRecordedAt, profile.CreatedAt,
        profile.UpdatedAt);
}
