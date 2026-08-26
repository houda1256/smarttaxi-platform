using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.MarkWaitingForParts;

public sealed record MarkWaitingForPartsCommand(Guid RequestId, Guid GarageUserId) : ICommand<Result>;
