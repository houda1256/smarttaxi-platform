using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.ApproveDriver;

public sealed record ApproveDriverCommand(Guid ReviewerId, Guid DriverProfileId) : ICommand<Result>;
