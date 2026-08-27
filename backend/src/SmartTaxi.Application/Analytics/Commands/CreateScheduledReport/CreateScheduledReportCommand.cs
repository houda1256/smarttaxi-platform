using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Commands.CreateScheduledReport;

public sealed record CreateScheduledReportCommand(
    ScheduledReportCategory Category, ScheduledReportFrequency Frequency, Guid RecipientUserId, DateTime FirstRunAtUtc)
    : ICommand<Result<Guid>>;
