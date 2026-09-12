using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.ReactivateCampaign;

/// <summary>Admin-only reversal of an administrative Suspend — an advertiser must never be able to reactivate their own suspended campaign (see the Module 8 audit's explicit rule).</summary>
public sealed class ReactivateCampaignCommandHandler : ICommandHandler<ReactivateCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être réactivée dans son état actuel.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public ReactivateCampaignCommandHandler(IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<Result> Handle(ReactivateCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, [AdCampaignStatus.Suspended], AdCampaignStatus.Active, touchReviewMetadata: true, command.AdminUserId,
            reviewReason: null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.AdminUserId, AdCampaignReviewAction.Reactivated, AdCampaignStatus.Suspended, AdCampaignStatus.Active,
                null, utcNow),
            cancellationToken);

        return Result.Success();
    }
}
