using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Commands.UpdateScheduledReport;

public sealed record UpdateScheduledReportCommand(
    Guid Id, ScheduledReportCategory Category, ScheduledReportFrequency Frequency, Guid RecipientUserId) : ICommand<Result>;
