using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.MarkVehicleReceived;

public sealed record MarkVehicleReceivedCommand(Guid RequestId, Guid GarageUserId) : ICommand<Result>;
