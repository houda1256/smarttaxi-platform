using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.FindSharedRideMatch;

public sealed record FindSharedRideMatchCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result<Guid?>>;
