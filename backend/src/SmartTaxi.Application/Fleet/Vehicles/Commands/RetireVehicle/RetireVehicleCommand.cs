using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RetireVehicle;

public sealed record RetireVehicleCommand(Guid RequestingUserId, Guid VehicleId) : ICommand<Result>;
