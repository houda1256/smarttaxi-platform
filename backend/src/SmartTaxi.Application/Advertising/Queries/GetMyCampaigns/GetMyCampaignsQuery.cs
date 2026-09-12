using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetMyCampaigns;

public sealed record GetMyCampaignsQuery(Guid AdvertiserUserId, int PageNumber, int PageSize) : IQuery<PagedResult<AdCampaign>>;
