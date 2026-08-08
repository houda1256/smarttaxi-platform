using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SuspendDriver;

public sealed record SuspendDriverCommand(Guid ReviewerId, Guid DriverProfileId) : ICommand<Result>;
