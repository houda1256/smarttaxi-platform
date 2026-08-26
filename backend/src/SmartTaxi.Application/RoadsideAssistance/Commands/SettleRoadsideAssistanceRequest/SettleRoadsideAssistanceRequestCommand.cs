using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;

public sealed record SettleRoadsideAssistanceRequestCommand(Guid RequestId, Guid AdminUserId) : ICommand<Result>;
