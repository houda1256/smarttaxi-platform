using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetMyCampaigns;

public sealed class GetMyCampaignsQueryHandler : IQueryHandler<GetMyCampaignsQuery, PagedResult<AdCampaign>>
{
    private readonly IAdCampaignRepository _repository;

    public GetMyCampaignsQueryHandler(IAdCampaignRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AdCampaign>> Handle(GetMyCampaignsQuery query, CancellationToken cancellationToken) =>
        _repository.GetForAdvertiserAsync(query.AdvertiserUserId, query.PageNumber, query.PageSize, cancellationToken);
}
