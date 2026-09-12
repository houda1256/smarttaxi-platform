using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.DriverAcceptRide;

public sealed record DriverAcceptRideCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
