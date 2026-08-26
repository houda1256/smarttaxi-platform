using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.SubmitCampaign;

/// <summary>Allowed from Draft or ChangesRequested only — a campaign needs at least one uploaded creative before it can meaningfully be reviewed.</summary>
public sealed class SubmitCampaignCommandHandler : ICommandHandler<SubmitCampaignCommand, Result>
{
    private const string NotFoundError = "Campagne introuvable.";
    private const string NotOwnerError = "Vous ne pouvez soumettre que vos propres campagnes.";
    private const string NoMediaError = "Un média doit être ajouté avant de soumettre la campagne.";
    private const string InvalidTransitionError = "Cette campagne ne peut pas être soumise dans son état actuel.";

    private static readonly IReadOnlyCollection<AdCampaignStatus> AllowedFromStatuses = [AdCampaignStatus.Draft, AdCampaignStatus.ChangesRequested];

    private readonly IAdCampaignRepository _campaignRepository;
    private readonly ICampaignCreativeRepository _creativeRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SubmitCampaignCommandHandler(
        IAdCampaignRepository campaignRepository, ICampaignCreativeRepository creativeRepository,
        IAdCampaignReviewHistoryRepository reviewHistoryRepository, INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _creativeRepository = creativeRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(SubmitCampaignCommand command, CancellationToken cancellationToken)
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

        var creatives = await _creativeRepository.GetForCampaignAsync(command.CampaignId, cancellationToken);

        if (creatives.Count == 0)
        {
            return Result.Failure(NoMediaError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var originalStatus = campaign.Status;

        var transitioned = await _campaignRepository.TryTransitionAsync(
            campaign.Id, AllowedFromStatuses, AdCampaignStatus.Submitted, touchReviewMetadata: true, reviewedByUserId: null,
            reviewReason: null, utcNow, cancellationToken);

        if (!transitioned)
        {
            return Result.Failure(InvalidTransitionError, ErrorType.Conflict);
        }

        await _reviewHistoryRepository.AddAsync(
            AdCampaignReviewHistoryEntry.Record(
                campaign.Id, command.RequestingUserId, AdCampaignReviewAction.Submitted, originalStatus, AdCampaignStatus.Submitted, null, utcNow),
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-submitted",
                new Dictionary<string, string> { ["CampaignName"] = campaign.Name }, IsMandatory: false, SourceType: "AdvertisingCampaign",
                SourceId: campaign.Id),
            cancellationToken);

        return Result.Success();
    }
}
