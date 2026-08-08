using SmartTaxi.Application.Fleet.Drivers;

namespace SmartTaxi.API.Contracts.Fleet.Drivers;

public sealed record DriverProfileResponse(
    Guid Id,
    Guid UserId,
    string DriverLicenseNumber,
    DateTime DriverLicenseExpiration,
    string? TaxiLicenseNumber,
    string VerificationStatus,
    string AvailabilityStatus,
    bool IndependentDriver,
    Guid? CurrentVehicleId,
    decimal AverageRating,
    int CompletedRideCount,
    int CancellationCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static DriverProfileResponse FromSummary(DriverProfileSummary summary) => new(
        summary.Id, summary.UserId, summary.DriverLicenseNumber, summary.DriverLicenseExpiration,
        summary.TaxiLicenseNumber, summary.VerificationStatus.ToString(), summary.AvailabilityStatus.ToString(),
        summary.IndependentDriver, summary.CurrentVehicleId, summary.AverageRating, summary.CompletedRideCount,
        summary.CancellationCount, summary.CreatedAt, summary.UpdatedAt);
}
