using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles;

public sealed record VehicleSummary(
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
    FuelType FuelType,
    TransmissionType TransmissionType,
    int SeatCount,
    bool HasAirConditioning,
    bool IsAccessible,
    VehicleCategory VehicleCategory,
    VehicleOperationalStatus OperationalStatus,
    VehicleVerificationStatus VerificationStatus,
    bool HasMainPhoto,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // MainPhotoReference is a storage key — never surfaced directly, same
    // rule as every document's FileReference. Callers get a boolean and use
    // the controlled photo-download endpoint instead.
    public static VehicleSummary FromEntity(Vehicle vehicle) => new(
        vehicle.Id, vehicle.OwnerId, vehicle.FleetId, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.Color,
        vehicle.LicensePlate, vehicle.Vin, vehicle.CurrentMileage, vehicle.FuelType, vehicle.TransmissionType,
        vehicle.SeatCount, vehicle.HasAirConditioning, vehicle.IsAccessible, vehicle.VehicleCategory,
        vehicle.OperationalStatus, vehicle.VerificationStatus, vehicle.MainPhotoReference is not null,
        vehicle.CreatedAt, vehicle.UpdatedAt);
}
