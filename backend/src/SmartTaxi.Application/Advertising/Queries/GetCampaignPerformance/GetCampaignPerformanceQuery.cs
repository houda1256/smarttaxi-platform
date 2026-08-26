using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;

public sealed record GetCampaignPerformanceQuery(Guid CampaignId, Guid RequestingUserId) : IQuery<Result<CampaignPerformanceSummary>>;
