using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SetDriverAvailability;

public sealed record SetDriverAvailabilityCommand(Guid RequestingUserId, Guid DriverProfileId, DriverAvailabilityStatus Status)
    : ICommand<Result>;
