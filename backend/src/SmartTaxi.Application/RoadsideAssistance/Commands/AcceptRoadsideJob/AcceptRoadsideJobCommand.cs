using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.AcceptRoadsideJob;

public sealed record AcceptRoadsideJobCommand(Guid RequestId, Guid PartnerUserId, decimal? EstimatedCost) : ICommand<Result>;
