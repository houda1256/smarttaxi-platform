using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.DriverArrived;

public sealed record DriverArrivedCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
