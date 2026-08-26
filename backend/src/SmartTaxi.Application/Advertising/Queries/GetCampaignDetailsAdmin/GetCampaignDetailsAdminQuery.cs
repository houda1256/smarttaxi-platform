using SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignDetailsAdmin;

public sealed record GetCampaignDetailsAdminQuery(Guid CampaignId) : IQuery<CampaignDetails?>;
