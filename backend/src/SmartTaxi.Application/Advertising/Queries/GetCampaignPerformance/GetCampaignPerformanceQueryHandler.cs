using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;

public sealed class GetCampaignPerformanceQueryHandler : IQueryHandler<GetCampaignPerformanceQuery, Result<CampaignPerformanceSummary>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez consulter que la performance de vos propres campagnes.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdvertisingImpressionRepository _impressionRepository;
    private readonly IAdvertisingClickRepository _clickRepository;

    public GetCampaignPerformanceQueryHandler(
        IAdCampaignRepository campaignRepository, IAdvertisingImpressionRepository impressionRepository,
        IAdvertisingClickRepository clickRepository)
    {
        _campaignRepository = campaignRepository;
        _impressionRepository = impressionRepository;
        _clickRepository = clickRepository;
    }

    public async Task<Result<CampaignPerformanceSummary>> Handle(GetCampaignPerformanceQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(query.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<CampaignPerformanceSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId != query.RequestingUserId)
        {
            return Result<CampaignPerformanceSummary>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var impressions = await _impressionRepository.CountForCampaignAsync(query.CampaignId, cancellationToken);
        var clicks = await _clickRepository.CountForCampaignAsync(query.CampaignId, cancellationToken);
        var ctr = impressions == 0 ? 0m : Math.Round((decimal)clicks / impressions, 4);

        return Result<CampaignPerformanceSummary>.Success(
            new CampaignPerformanceSummary(campaign.Id, impressions, clicks, ctr, campaign.BudgetLimit, campaign.ConsumedBudget, campaign.RemainingBudget));
    }
}
