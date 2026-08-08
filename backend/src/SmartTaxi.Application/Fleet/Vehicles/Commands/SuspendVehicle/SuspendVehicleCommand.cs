using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.SuspendVehicle;

public sealed record SuspendVehicleCommand(Guid RequestingUserId, Guid VehicleId) : ICommand<Result>;
