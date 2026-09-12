using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RegisterVehicle;

public sealed record RegisterVehicleCommand(
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
    VehicleCategory VehicleCategory) : ICommand<Result<Guid>>;
