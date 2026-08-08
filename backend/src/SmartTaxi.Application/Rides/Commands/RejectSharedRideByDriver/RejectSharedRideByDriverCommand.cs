using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.RejectSharedRideByDriver;

public sealed record RejectSharedRideByDriverCommand(Guid RequestingUserId, Guid MatchId) : ICommand<Result>;
