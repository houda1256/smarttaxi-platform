using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.AcceptFare;

public sealed record AcceptFareCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result<decimal>>;
