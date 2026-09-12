using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.DisputeRoadsideAssistanceRequest;

public sealed record DisputeRoadsideAssistanceRequestCommand(Guid RequestId, Guid AdminUserId) : ICommand<Result>;
