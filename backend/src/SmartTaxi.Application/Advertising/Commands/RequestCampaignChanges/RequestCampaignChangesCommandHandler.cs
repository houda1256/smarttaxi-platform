using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RequestCampaignChanges;

/// <summary>Unlike Reject, ChangesRequested is not terminal — the advertiser edits (UpdateCampaignCommand, still allowed from ChangesRequested) and resubmits (SubmitCampaignCommand). Reason is mandatory, same as Reject.</summary>
public sealed class RequestCampaignChangesCommandHandler : ICommandHandler<RequestCampaignChangesCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string SelfReviewError = "Un annonceur ne peut pas demander des modifications sur sa propre campagne.";
    private const string ReasonRequiredError = "Un motif est requis.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas recevoir de demande de modification dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses = [AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RequestCampaignChangesCommandHandler(
        IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RequestCampaignChangesCommand command, CancellationToken cancellationToken)
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
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.ChangesRequested, touchReviewMetadata: true, command.ReviewerUserId,
            command.Reason, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.ReviewerUserId, AdCampaignReviewAction.ChangesRequested, originalStatus,
                AdCampaignStatus.ChangesRequested, command.Reason, utcNow),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.changes-requested",
                new Dictionary<string, string> { ["CampaignName"] = campaign.Name, ["Reason"] = command.Reason }, IsMandatory: false,
                SourceType: "AdvertisingCampaign", SourceId: campaign.Id),
            cancellationToken);

        return Result.Success();
    }
}
