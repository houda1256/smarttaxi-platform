using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.SelectRoadsidePartner;

public sealed record SelectRoadsidePartnerCommand(Guid RequestId, Guid RequesterUserId, Guid PartnerUserId) : ICommand<Result>;
