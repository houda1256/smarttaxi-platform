using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Commands.RejectVehicle;

public sealed record RejectVehicleCommand(Guid ReviewerId, Guid VehicleId, string Reason) : ICommand<Result>;
