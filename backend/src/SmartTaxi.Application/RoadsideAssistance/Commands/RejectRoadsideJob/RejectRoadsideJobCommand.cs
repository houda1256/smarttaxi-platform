using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.RejectRoadsideJob;

public sealed record RejectRoadsideJobCommand(Guid RequestId, Guid PartnerUserId, string RejectionReason) : ICommand<Result>;
