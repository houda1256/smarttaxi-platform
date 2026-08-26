using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Advertising.Commands.ProcessCompletedCampaigns;

/// <summary>Explicit, externally-triggered sweep — every Active campaign whose EndAtUtc has passed becomes Completed. Financial settlement is a separate, explicit admin action (SettleCampaignBudgetCommand), not performed here.</summary>
public sealed class ProcessCompletedCampaignsCommandHandler : ICommandHandler<ProcessCompletedCampaignsCommand, Result<int>>
{
    private readonly IAdCampaignRepository _campaignRepository;
    private readonly IAdCampaignReviewHistoryRepository _reviewHistoryRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ProcessCompletedCampaignsCommandHandler(
        IAdCampaignRepository campaignRepository, IAdCampaignReviewHistoryRepository reviewHistoryRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _campaignRepository = campaignRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<int>> Handle(ProcessCompletedCampaignsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var completedCount = 0;

        foreach (var campaign in await _campaignRepository.GetDueForCompletionAsync(utcNow, cancellationToken))
        {
            if (await _campaignRepository.TryTransitionAsync(
                    campaign.Id, [AdCampaignStatus.Active], AdCampaignStatus.Completed, touchReviewMetadata: false, null, null, utcNow,
                    cancellationToken))
            {
                await _reviewHistoryRepository.AddAsync(
                    AdCampaignReviewHistoryEntry.Record(
                        campaign.Id, null, AdCampaignReviewAction.Completed, AdCampaignStatus.Active, AdCampaignStatus.Completed, null, utcNow),
                    cancellationToken);

                await _notificationDispatcher.DispatchAsync(
                    new NotificationRequest(
                        campaign.AdvertiserUserId, NotificationCategory.Advertising, "advertising.campaign-completed",
                        new Dictionary<string, string> { ["CampaignName"] = campaign.Name }, IsMandatory: false,
                        SourceType: "AdvertisingCampaign", SourceId: campaign.Id),
                    cancellationToken);

                completedCount++;
            }
        }

        return Result<int>.Success(completedCount);
    }
}
