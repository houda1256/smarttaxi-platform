using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of the real repository's atomic conditional transition/budget-consumption guards — same "reflection SetProperty helper" convention as FakeProfessionalAccountRequestRepository, since AdCampaign deliberately exposes no public status-mutation method.</summary>
public sealed class FakeAdCampaignRepository : IAdCampaignRepository
{
    private readonly Dictionary<Guid, AdCampaign> _campaigns = new();

    public Task<AdCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_campaigns.GetValueOrDefault(campaignId));

    public Task<PagedResult<AdCampaign>> GetForAdvertiserAsync(Guid advertiserUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _campaigns.Values.Where(c => c.AdvertiserUserId == advertiserUserId).ToList();
        return Task.FromResult(new PagedResult<AdCampaign>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<AdCampaign>> GetPendingReviewAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _campaigns.Values.Where(c => c.Status is AdCampaignStatus.Submitted or AdCampaignStatus.UnderReview).ToList();
        return Task.FromResult(new PagedResult<AdCampaign>(items, items.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<AdCampaign>> GetApprovedAwaitingScheduleAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdCampaign>>(_campaigns.Values.Where(c => c.Status == AdCampaignStatus.Approved).ToList());

    public Task<IReadOnlyCollection<AdCampaign>> GetDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdCampaign>>(
            _campaigns.Values.Where(c => c.Status == AdCampaignStatus.Scheduled && c.StartAtUtc <= utcNow).ToList());

    public Task<IReadOnlyCollection<AdCampaign>> GetDueForCompletionAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdCampaign>>(
            _campaigns.Values.Where(c => c.Status == AdCampaignStatus.Active && c.EndAtUtc <= utcNow).ToList());

    public Task AddAsync(AdCampaign campaign, CancellationToken cancellationToken)
    {
        _campaigns[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AdCampaign campaign, CancellationToken cancellationToken)
    {
        _campaigns[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task<bool> TryTransitionAsync(
        Guid campaignId, IReadOnlyCollection<AdCampaignStatus> allowedFromStatuses, AdCampaignStatus newStatus, bool touchReviewMetadata,
        Guid? reviewedByUserId, string? reviewReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign) || !allowedFromStatuses.Contains(campaign.Status))
        {
            return Task.FromResult(false);
        }

        SetProperty(campaign, nameof(AdCampaign.Status), newStatus);
        SetProperty(campaign, nameof(AdCampaign.UpdatedAtUtc), utcNow);

        if (newStatus == AdCampaignStatus.Submitted)
        {
            SetProperty(campaign, nameof(AdCampaign.SubmittedAtUtc), utcNow);
        }

        if (touchReviewMetadata)
        {
            SetProperty(campaign, nameof(AdCampaign.ReviewedByUserId), reviewedByUserId);
            SetProperty(campaign, nameof(AdCampaign.ReviewedAtUtc), reviewedByUserId is not null ? utcNow : (DateTime?)null);
            SetProperty(campaign, nameof(AdCampaign.ReviewReason), reviewReason);
        }

        return Task.FromResult(true);
    }

    public Task<bool> TryConsumeBudgetAsync(Guid campaignId, decimal amount, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign) || campaign.Status != AdCampaignStatus.Active)
        {
            return Task.FromResult(false);
        }

        if (amount == 0)
        {
            return Task.FromResult(true);
        }

        if (campaign.ConsumedBudget + amount > campaign.BudgetLimit)
        {
            return Task.FromResult(false);
        }

        if (campaign.DailyBudgetLimit is not null)
        {
            var dailyConsumed = campaign.DailyConsumedDateUtc == utcNow.Date ? campaign.DailyConsumedBudget + amount : amount;

            if (dailyConsumed > campaign.DailyBudgetLimit)
            {
                return Task.FromResult(false);
            }

            SetProperty(campaign, nameof(AdCampaign.DailyConsumedBudget), dailyConsumed);
            SetProperty(campaign, nameof(AdCampaign.DailyConsumedDateUtc), utcNow.Date);
        }

        SetProperty(campaign, nameof(AdCampaign.ConsumedBudget), campaign.ConsumedBudget + amount);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkSettledAsync(Guid campaignId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_campaigns.TryGetValue(campaignId, out var campaign) || campaign.SettledAtUtc is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(campaign, nameof(AdCampaign.SettledAtUtc), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(AdCampaign campaign, string propertyName, object? value) =>
        typeof(AdCampaign).GetProperty(propertyName)!.SetValue(campaign, value);
}
