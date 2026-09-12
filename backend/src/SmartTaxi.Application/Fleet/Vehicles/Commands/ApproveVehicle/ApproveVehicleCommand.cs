using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.ApproveVehicle;

public sealed record ApproveVehicleCommand(Guid ReviewerId, Guid VehicleId) : ICommand<Result>;
