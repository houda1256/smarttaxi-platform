using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.ApproveCampaign;

/// <summary>Allowed from Submitted or UnderReview only. An advertiser must never approve their own campaign — enforced explicitly here, never just implied by permission scoping. Approving a campaign also approves every one of its still-PendingReview creatives (there is no separate per-creative admin approval step in this pass).</summary>
public sealed class ApproveCampaignCommandHandler : ICommandHandler<ApproveCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string SelfReviewError = "Un annonceur ne peut pas approuver sa propre campagne.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être approuvée dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses = [AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ApproveCampaignCommandHandler(
        IAdCampaignRepository campaignRepository, ICampaignCreativeRepository creativeRepository,
        IAdCampaignReviewHistoryRepository reviewHistoryRepository, INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ApproveCampaignCommand command, CancellationToken cancellationToken)
    {
        var campaign = await _campaignRepository.GetByIdAsync(command.CampaignId, cancellationToken);

        if (campaign is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (campaign.AdvertiserUserId == command.ReviewerUserId)
        {
            return Result.Failure(SelfReviewError, ErrorType.Forbidden);
        }

        var utcNow = DateTime.UtcNow;
        var originalStatus = campaign.Status;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.Approved, touchReviewMetadata: true, command.ReviewerUserId, reviewReason: null,
            utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        foreach (var creative in await _creativeRepository.GetForCampaignAsync(command.CampaignId, cancellationToken))
        {
            if (creative.Status == AdMediaStatus.PendingReview)
            {
                await _creativeRepository.TryReviewAsync(creative.Id, approved: true, command.ReviewerUserId, null, utcNow, cancellationToken);
            }
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.ReviewerUserId, AdCampaignReviewAction.Approved, originalStatus, AdCampaignStatus.Approved, null, utcNow),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-approved",
                new Dictionary<string, string> { ["CampaignName"] = campaign.Name }, IsMandatory: false, SourceType: "AdvertisingCampaign",
                SourceId: campaign.Id),
            cancellationToken);

        return Result.Success();
    }
}
