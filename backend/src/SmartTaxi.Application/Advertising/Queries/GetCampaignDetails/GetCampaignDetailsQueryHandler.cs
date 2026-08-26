using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;

/// <summary>Self-service — an advertiser may never read another advertiser's campaign, enforced here (not just by permission scoping).</summary>
public sealed class GetCampaignDetailsQueryHandler : IQueryHandler<GetCampaignDetailsQuery, Result<CampaignDetails>>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez consulter que vos propres campagnes.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public GetCampaignDetailsQueryHandler(
        IAdCampaignRepository campaignRepository, ICampaignCreativeRepository creativeRepository,
        IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<Result<CampaignDetails>> Handle(GetCampaignDetailsQuery query, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(query.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result<CampaignDetails>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId != query.RequestingUserId)
        {
            return Result<CampaignDetails>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var creatives = await _creativeRepository.GetForCampaignAsync(query.CampaignId, cancellationToken);
        var history = await _reviewHistoryRepository.GetForCampaignAsync(query.CampaignId, cancellationToken);

        return Result<CampaignDetails>.Success(new CampaignDetails(campaign, creatives, history));
    }
}
