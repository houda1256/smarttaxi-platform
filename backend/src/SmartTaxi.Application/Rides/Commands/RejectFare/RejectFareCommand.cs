using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.RejectFare;

public sealed record RejectFareCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
