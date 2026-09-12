using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CancelRoadsideAssistanceRequest;

public sealed record CancelRoadsideAssistanceRequestCommand(Guid RequestId, Guid RequesterUserId, string? Reason) : ICommand<Result>;
