using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerArrived;

public sealed record MarkPartnerArrivedCommand(Guid RequestId, Guid PartnerUserId) : ICommand<Result>;
