using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ExpireStaleRoadsideRequests;

public sealed record ExpireStaleRoadsideRequestsCommand : ICommand<Result<int>>;
