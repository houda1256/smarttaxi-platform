using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ReportRideMessage;

public sealed record ReportRideMessageCommand(Guid RequestingUserId, Guid RideId, Guid MessageId) : ICommand<Result>;
