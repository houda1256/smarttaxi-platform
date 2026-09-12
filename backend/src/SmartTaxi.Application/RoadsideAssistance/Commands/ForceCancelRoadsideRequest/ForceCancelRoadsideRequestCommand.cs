using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ForceCancelRoadsideRequest;

public sealed record ForceCancelRoadsideRequestCommand(Guid RequestId, Guid AdminUserId, string Reason) : ICommand<Result>;
