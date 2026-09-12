using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByDriver;

public sealed record CancelRideByDriverCommand(
    Guid RequestingUserId, Guid RideId, DriverCancellationReason Reason, string? Details) : ICommand<Result>;
