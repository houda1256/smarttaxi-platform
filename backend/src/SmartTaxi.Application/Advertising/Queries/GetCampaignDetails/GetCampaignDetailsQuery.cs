using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;

public sealed record GetCampaignDetailsQuery(Guid CampaignId, Guid RequestingUserId) : IQuery<Result<CampaignDetails>>;
