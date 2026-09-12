using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignPerformanceAdmin;

public sealed class GetCampaignPerformanceAdminQueryHandler : IQueryHandler<GetCampaignPerformanceAdminQuery, CampaignPerformanceSummary?>
{
    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingImpressionRepository _impressionRepository;
    private readonly IAdvertisingClickRepository _clickRepository;

    public GetCampaignPerformanceAdminQueryHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingImpressionRepository impressionRepository,
        IAdvertisingClickRepository clickRepository)
    {
        _campaignRepository = campaignRepository;
        _impressionRepository = impressionRepository;
        _clickRepository = clickRepository;
    }

    public async Task<CampaignPerformanceSummary?> Handle(GetCampaignPerformanceAdminQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(query.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return null;
        }

        var impressions = await _impressionRepository.CountForCampaignAsync(query.CampaignId, cancellationToken);
        var clicks = await _clickRepository.CountForCampaignAsync(query.CampaignId, cancellationToken);
        var ctr = impressions == 0 ? 0m : Math.Round((decimal)clicks / impressions, 4);

        return new CampaignPerformanceSummary(campaign.Id, impressions, clicks, ctr, campaign.BudgetLimit, campaign.ConsumedBudget, campaign.RemainingBudget);
    }
}
