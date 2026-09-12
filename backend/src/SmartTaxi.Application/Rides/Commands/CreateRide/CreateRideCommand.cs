using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CreateRide;

public sealed record CreateRideCommand(
    Guid CustomerId,
    RideType RideType,
    string PickupAddress,
    double PickupLatitude,
    double PickupLongitude,
    string DestinationAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    DateTime? ScheduledAt,
    int PassengerCount,
    int LuggageCount,
    bool NeedsAirConditioning,
    bool NeedsAccessibleVehicle,
    bool HasChildSeatRequest,
    bool HasPet,
    VehicleCategory? PreferredVehicleCategory,
    RidePaymentMethod PreferredPaymentMethod,
    string? SpecialInstructions) : ICommand<Result<Guid>>;
