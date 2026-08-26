using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.CancelCampaign;

/// <summary>Self-service cancellation — never available once a campaign is Active (use Pause) or already Suspended/Completed/Cancelled/Rejected. An Active campaign's administrative stop is Suspend, not Cancel.</summary>
public sealed class CancelCampaignCommandHandler : ICommandHandler<CancelCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez annuler que vos propres campagnes.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être annulée dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses =
    [
        AdCampaignStatus.Draft, AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview, AdCampaignStatus.ChangesRequested,
        AdCampaignStatus.Approved, AdCampaignStatus.Scheduled, AdCampaignStatus.Paused
    ];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;

    public CancelCampaignCommandHandler(IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<Result> Handle(CancelCampaignCommand command, CancellationToken cancellationToken)
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
        var originalStatus = campaign.Status;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.Cancelled, touchReviewMetadata: false, reviewedByUserId: null,
            reviewReason: null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.RequestingUserId, AdCampaignReviewAction.Cancelled, originalStatus, AdCampaignStatus.Cancelled, null, utcNow),
            cancellationToken);

        return Result.Success();
    }
}
