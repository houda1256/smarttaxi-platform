using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.DriverRejectRide;

public sealed record DriverRejectRideCommand(Guid RequestingUserId, Guid RideId, string? Reason) : ICommand<Result>;
