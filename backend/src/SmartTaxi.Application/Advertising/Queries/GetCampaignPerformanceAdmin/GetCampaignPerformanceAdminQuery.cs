using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignPerformanceAdmin;

public sealed record GetCampaignPerformanceAdminQuery(Guid CampaignId) : IQuery<CampaignPerformanceSummary?>;
