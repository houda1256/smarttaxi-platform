using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.StartRoadsideIntervention;

public sealed record StartRoadsideInterventionCommand(Guid RequestId, Guid PartnerUserId) : ICommand<Result>;
