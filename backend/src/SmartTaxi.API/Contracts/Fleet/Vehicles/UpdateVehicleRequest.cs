namespace SmartTaxi.API.Contracts.Fleet.Vehicles;

public sealed record UpdateVehicleRequest(
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
    string VehicleCategory);
