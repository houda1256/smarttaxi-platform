using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CompleteRide;

public sealed record CompleteRideCommand(Guid RequestingUserId, Guid RideId, decimal ActualDistanceKm, int ActualDurationMinutes)
    : ICommand<Result<decimal>>;
