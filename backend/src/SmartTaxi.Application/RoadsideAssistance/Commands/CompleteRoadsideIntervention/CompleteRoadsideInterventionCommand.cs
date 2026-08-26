using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.CompleteRoadsideIntervention;

public sealed record CompleteRoadsideInterventionCommand(Guid RequestId, Guid PartnerUserId, decimal FinalCost) : ICommand<Result>;
