using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Entities;

namespace SmartTaxi.Application.Analytics.Queries.GetScheduledReports;

public sealed record GetScheduledReportsQuery(int PageNumber, int PageSize) : IQuery<PagedResult<ScheduledReportDefinition>>;
