using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.API.Contracts.Fleet.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    Guid OwnerId,
    Guid? FleetId,
    string Brand,
    string Model,
    int Year,
    string Color,
    string LicensePlate,
    string? Vin,
    int CurrentMileage,
    string FuelType,
    string TransmissionType,
    int SeatCount,
    bool HasAirConditioning,
    bool IsAccessible,
    string VehicleCategory,
    string OperationalStatus,
    string VerificationStatus,
    bool HasMainPhoto,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static VehicleResponse FromSummary(VehicleSummary summary) => new(
        summary.Id, summary.OwnerId, summary.FleetId, summary.Brand, summary.Model, summary.Year, summary.Color,
        summary.LicensePlate, summary.Vin, summary.CurrentMileage, summary.FuelType.ToString(),
        summary.TransmissionType.ToString(), summary.SeatCount, summary.HasAirConditioning, summary.IsAccessible,
        summary.VehicleCategory.ToString(), summary.OperationalStatus.ToString(), summary.VerificationStatus.ToString(),
        summary.HasMainPhoto, summary.CreatedAt, summary.UpdatedAt);
}
