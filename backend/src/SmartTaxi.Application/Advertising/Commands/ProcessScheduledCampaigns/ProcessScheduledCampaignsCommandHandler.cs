using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.ProcessScheduledCampaigns;

/// <summary>
/// No scheduler infrastructure exists in this codebase — this is an explicit,
/// externally-triggered sweep (same convention as ProcessExpiredPoints/
/// ProcessDueNotifications), safe to call repeatedly. Two independent stages,
/// each idempotent via its own atomic conditional transition: (1) every
/// Approved campaign immediately becomes Scheduled — nothing further gates it
/// once approved in this digital-only phase; (2) every Scheduled campaign
/// whose StartAtUtc has arrived becomes Active.
/// </summary>
public sealed class ProcessScheduledCampaignsCommandHandler : ICommandHandler<ProcessScheduledCampaignsCommand, Result<int>>
{
    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ProcessScheduledCampaignsCommandHandler(
        IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<int>> Handle(ProcessScheduledCampaignsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var transitionedCount = 0;

        foreach (var campaign in await _campaignRepository.GetApprovedAwaitingScheduleAsync(cancellationToken))
        {
            if (await _campaignRepository.TryTransitionAsync(
                    campaign.Id, [AdCampaignStatus.Approved], AdCampaignStatus.Scheduled, touchReviewMetadata: false, null, null, utcNow,
                    cancellationToken))
            {
                await _reviewHistoryRepository.AddAsync(
                    AdCampaignReviewHistoryEntry.Record(
                        campaign.Id, null, AdCampaignReviewAction.Scheduled, AdCampaignStatus.Approved, AdCampaignStatus.Scheduled, null, utcNow),
                    cancellationToken);
                transitionedCount++;
            }
        }

        foreach (var campaign in await _campaignRepository.GetDueForActivationAsync(utcNow, cancellationToken))
        {
            if (await _campaignRepository.TryTransitionAsync(
                    campaign.Id, [AdCampaignStatus.Scheduled], AdCampaignStatus.Active, touchReviewMetadata: false, null, null, utcNow,
                    cancellationToken))
            {
                await _reviewHistoryRepository.AddAsync(
                    AdCampaignReviewHistoryEntry.Record(
                        campaign.Id, null, AdCampaignReviewAction.Activated, AdCampaignStatus.Scheduled, AdCampaignStatus.Active, null, utcNow),
                    cancellationToken);

                await _notificationDispatcher.DispatchAsync(
                    new NotificationRequest(
                        campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-active",
                        new Dictionary<string, string> { ["CampaignName"] = campaign.Name }, IsMandatory: false,
                        SourceType: "AdvertisingCampaign", SourceId: campaign.Id),
                    cancellationToken);

                transitionedCount++;
            }
        }

        return Result<int>.Success(transitionedCount);
    }
}
