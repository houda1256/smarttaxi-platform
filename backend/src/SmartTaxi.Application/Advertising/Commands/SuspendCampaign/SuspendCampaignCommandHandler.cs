using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.SuspendCampaign;

/// <summary>Administrative enforcement, never advertiser-initiated — permission-gated at the endpoint (advertising.campaigns.review). Mandatory reason, full audit history. Allowed from any non-terminal, non-Draft state.</summary>
public sealed class SuspendCampaignCommandHandler : ICommandHandler<SuspendCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string ReasonRequiredError = "Un motif de suspension est requis.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être suspendue dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses =
    [
        AdCampaignStatus.Submitted, AdCampaignStatus.UnderReview, AdCampaignStatus.Approved, AdCampaignStatus.Scheduled,
        AdCampaignStatus.Active, AdCampaignStatus.Paused
    ];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SuspendCampaignCommandHandler(
        IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(SuspendCampaignCommand command, CancellationToken cancellationToken)
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

        var utcNow = DateTime.UtcNow;
        var originalStatus = campaign.Status;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.Suspended, touchReviewMetadata: true, command.AdminUserId, command.Reason,
            utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.AdminUserId, AdCampaignReviewAction.Suspended, originalStatus, AdCampaignStatus.Suspended,
                command.Reason, utcNow),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-suspended",
                new Dictionary<string, string> { ["CampaignName"] = campaign.Name, ["Reason"] = command.Reason }, IsMandatory: false,
                SourceType: "AdvertisingCampaign", SourceId: campaign.Id),
            cancellationToken);

        return Result.Success();
    }
}
