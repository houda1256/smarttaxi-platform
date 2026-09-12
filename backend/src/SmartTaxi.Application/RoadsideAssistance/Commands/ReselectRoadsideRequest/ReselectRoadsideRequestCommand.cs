using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ReselectRoadsideRequest;

public sealed record ReselectRoadsideRequestCommand(Guid RequestId, Guid RequesterUserId) : ICommand<Result>;
