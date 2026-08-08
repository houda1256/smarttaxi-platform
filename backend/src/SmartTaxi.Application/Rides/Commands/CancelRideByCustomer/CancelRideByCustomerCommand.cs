using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByCustomer;

public sealed record CancelRideByCustomerCommand(
    Guid RequestingUserId, Guid RideId, CustomerCancellationReason Reason, string? Details) : ICommand<Result<decimal>>;
