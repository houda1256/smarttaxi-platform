using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.PauseCampaign;

/// <summary>Advertiser-initiated, authorized temporary stop — distinct from Suspend (administrative enforcement). Only from Active.</summary>
public sealed class PauseCampaignCommandHandler : ICommandHandler<PauseCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez mettre en pause que vos propres campagnes.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être mise en pause dans son état actuel.";

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public PauseCampaignCommandHandler(IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<Result> Handle(PauseCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var utcNow = DateTime.UtcNow;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, [AdCampaignStatus.Active], AdCampaignStatus.Paused, touchReviewMetadata: false, reviewedByUserId: null,
            reviewReason: null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.RequestingUserId, AdCampaignReviewAction.Paused, AdCampaignStatus.Active, AdCampaignStatus.Paused, null, utcNow),
            cancellationToken);

        return Result.Success();
    }
}
