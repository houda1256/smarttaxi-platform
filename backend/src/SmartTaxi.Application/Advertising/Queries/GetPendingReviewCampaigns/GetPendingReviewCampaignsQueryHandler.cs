using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPendingReviewCampaigns;

public sealed class GetPendingReviewCampaignsQueryHandler : IQueryHandler<GetPendingReviewCampaignsQuery, PagedResult<AdCampaign>>
{
    private readonly IAdCampaignRepository _repository;

    public GetPendingReviewCampaignsQueryHandler(IAdCampaignRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AdCampaign>> Handle(GetPendingReviewCampaignsQuery query, CancellationToken cancellationToken) =>
        _repository.GetPendingReviewAsync(query.PageNumber, query.PageSize, cancellationToken);
}
