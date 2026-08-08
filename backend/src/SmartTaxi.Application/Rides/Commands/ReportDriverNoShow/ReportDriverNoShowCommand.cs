using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ReportDriverNoShow;

public sealed record ReportDriverNoShowCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
