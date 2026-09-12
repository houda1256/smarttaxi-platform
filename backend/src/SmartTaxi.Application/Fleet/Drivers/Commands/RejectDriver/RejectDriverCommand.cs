using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.RejectDriver;

public sealed record RejectDriverCommand(Guid ReviewerId, Guid DriverProfileId, string Reason) : ICommand<Result>;
