using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CustomerRejectSharedRide;

public sealed record CustomerRejectSharedRideCommand(Guid RequestingUserId, Guid MatchId) : ICommand<Result>;
