using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPendingReviewCampaigns;

public sealed record GetPendingReviewCampaignsQuery(int PageNumber, int PageSize) : IQuery<PagedResult<AdCampaign>>;
