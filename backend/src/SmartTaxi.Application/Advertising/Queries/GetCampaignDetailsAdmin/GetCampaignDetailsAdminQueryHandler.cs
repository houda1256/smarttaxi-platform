using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignDetailsAdmin;

public sealed class GetCampaignDetailsAdminQueryHandler : IQueryHandler<GetCampaignDetailsAdminQuery, CampaignDetails?>
{
    private readonly IAdCampaignRepository _campaignRepository;
    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public GetCampaignDetailsAdminQueryHandler(
        IAdCampaignRepository campaignRepository, ICampaignCreativeRepository creativeRepository,
        IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<CampaignDetails?> Handle(GetCampaignDetailsAdminQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(query.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return null;
        }

        var creatives = await _creativeRepository.GetForCampaignAsync(query.CampaignId, cancellationToken);
        var history = await _reviewHistoryRepository.GetForCampaignAsync(query.CampaignId, cancellationToken);

        return new CampaignDetails(campaign, creatives, history);
    }
}
