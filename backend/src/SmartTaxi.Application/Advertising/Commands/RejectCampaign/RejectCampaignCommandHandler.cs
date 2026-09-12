using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RejectCampaign;

/// <summary>Rejected is terminal in this pass — the spec supports a future Draft/revision workflow, not implemented here; the advertiser must create a new campaign. Reason is mandatory.</summary>
public sealed class RejectCampaignCommandHandler : ICommandHandler<RejectCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string SelfReviewError = "Un annonceur ne peut pas rejeter sa propre campagne.";
    private const string ReasonRequiredError = "Un motif de rejet est requis.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être rejetée dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses = [AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RejectCampaignCommandHandler(
        IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RejectCampaignCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

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
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.Rejected, touchReviewMetadata: true, command.ReviewerUserId, command.Reason,
            utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.ReviewerUserId, AdCampaignReviewAction.Rejected, originalStatus, AdCampaignStatus.Rejected,
                command.Reason, utcNow),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-rejected",
                new Dictionary<string, string> { ["CampaignName"] = campaign.Name, ["Reason"] = command.Reason }, IsMandatory: false,
                SourceType: "AdvertisingCampaign", SourceId: campaign.Id),
            cancellationToken);

        return Result.Success();
    }
}
