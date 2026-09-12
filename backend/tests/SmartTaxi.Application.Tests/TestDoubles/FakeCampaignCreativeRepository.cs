using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeCampaignCreativeRepository : ICampaignCreativeRepository
{
    private readonly Dictionary<Guid, CampaignCreative> _creatives = new();

    /// <summary>Test hook: when set, AddAsync/ReplaceAsync throw — simulates a DB persistence failure after a
    /// successful storage write, to prove the compensating cleanup (Module 8 audit fix #3) fires.</summary>
    public bool ThrowOnPersist { get; set; }

    public Task<CampaignCreative?> GetByIdAsync(Guid creativeId, CancellationToken cancellationToken) =>
        Task.FromResult(_creatives.GetValueOrDefault(creativeId));

    public Task<IReadOnlyCollection<CampaignCreative>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<CampaignCreative>>(_creatives.Values.Where(c => c.CampaignId == campaignId).OrderByDescending(c => c.Version).ToList());

    public Task<CampaignCreative?> GetCurrentForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_creatives.Values.Where(c => c.CampaignId == campaignId && c.Status != AdMediaStatus.Replaced).OrderByDescending(c => c.Version).FirstOrDefault());

    public Task AddAsync(CampaignCreative creative, CancellationToken cancellationToken)
    {
        if (ThrowOnPersist)
        {
            throw new InvalidOperationException("Simulated DB persistence failure (test).");
        }

        _creatives[creative.Id] = creative;
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(Guid previousCreativeId, CampaignCreative newVersion, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (ThrowOnPersist)
        {
            throw new InvalidOperationException("Simulated DB persistence failure (test).");
        }

        if (_creatives.TryGetValue(previousCreativeId, out var previous))
        {
            SetProperty(previous, nameof(CampaignCreative.Status), AdMediaStatus.Replaced);
        }

        _creatives[newVersion.Id] = newVersion;
        return Task.CompletedTask;
    }

    public Task<bool> TryReviewAsync(Guid creativeId, bool approved, Guid reviewerUserId, string? rejectionReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_creatives.TryGetValue(creativeId, out var creative) || creative.Status != AdMediaStatus.PendingReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(creative, nameof(CampaignCreative.Status), approved ? AdMediaStatus.Approved : AdMediaStatus.Rejected);
        SetProperty(creative, nameof(CampaignCreative.ReviewedByUserId), reviewerUserId);
        SetProperty(creative, nameof(CampaignCreative.ReviewedAtUtc), utcNow);
        SetProperty(creative, nameof(CampaignCreative.RejectionReason), rejectionReason);
        return Task.FromResult(true);
    }

    private static void SetProperty(CampaignCreative creative, string propertyName, object? value) =>
        typeof(CampaignCreative).GetProperty(propertyName)!.SetValue(creative, value);
}
