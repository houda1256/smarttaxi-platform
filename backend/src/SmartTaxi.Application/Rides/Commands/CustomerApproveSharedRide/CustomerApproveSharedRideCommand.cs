using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CustomerApproveSharedRide;

public sealed record CustomerApproveSharedRideCommand(Guid RequestingUserId, Guid MatchId) : ICommand<Result>;
