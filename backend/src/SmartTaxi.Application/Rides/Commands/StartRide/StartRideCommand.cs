using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.StartRide;

public sealed record StartRideCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
