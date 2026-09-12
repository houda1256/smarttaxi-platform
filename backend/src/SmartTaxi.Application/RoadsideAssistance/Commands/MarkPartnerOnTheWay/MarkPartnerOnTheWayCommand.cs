using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerOnTheWay;

public sealed record MarkPartnerOnTheWayCommand(Guid RequestId, Guid PartnerUserId) : ICommand<Result>;
