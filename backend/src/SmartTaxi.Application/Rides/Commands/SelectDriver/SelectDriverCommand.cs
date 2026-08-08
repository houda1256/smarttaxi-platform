using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.SelectDriver;

public sealed record SelectDriverCommand(Guid RequestingUserId, Guid RideId, Guid DriverId, Guid VehicleId) : ICommand<Result<Guid>>;
