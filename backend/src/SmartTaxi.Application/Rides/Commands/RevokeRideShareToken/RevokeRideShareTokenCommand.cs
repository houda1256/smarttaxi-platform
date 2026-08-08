using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.RevokeRideShareToken;

public sealed record RevokeRideShareTokenCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
