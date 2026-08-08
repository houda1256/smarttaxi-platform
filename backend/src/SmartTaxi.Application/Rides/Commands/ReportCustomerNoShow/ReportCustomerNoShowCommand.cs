using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ReportCustomerNoShow;

public sealed record ReportCustomerNoShowCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result>;
