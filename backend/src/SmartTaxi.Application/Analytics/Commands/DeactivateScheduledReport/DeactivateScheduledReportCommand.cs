using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Commands.DeactivateScheduledReport;

public sealed record DeactivateScheduledReportCommand(Guid Id) : ICommand<Result>;
