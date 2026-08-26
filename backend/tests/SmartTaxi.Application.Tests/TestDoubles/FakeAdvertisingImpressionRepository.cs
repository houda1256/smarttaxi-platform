using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of the real repository's atomic idempotency-check + budget-consumption + insert.</summary>
public sealed class FakeAdvertisingImpressionRepository : IAdvertisingImpressionRepository
{
    private readonly Dictionary<Guid, AdvertisingImpression> _impressions = new();
    private readonly FakeAdCampaignRepository _campaignRepository;

    public FakeAdvertisingImpressionRepository(FakeAdCampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public Task<AdvertisingImpression?> GetByIdAsync(Guid impressionId, CancellationToken cancellationToken) =>
        Task.FromResult(_impressions.GetValueOrDefault(impressionId));

    public Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_impressions.Values.Count(i => i.CampaignId == campaignId));

    public async Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var existing = _impressions.Values.FirstOrDefault(i => i.IdempotencyKey == idempotencyKey);

        if (existing is not null)
        {
            return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Replayed, existing.Id);
        }

        if (!await _campaignRepository.TryConsumeBudgetAsync(campaignId, operationalCost, utcNow, cancellationToken))
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);
            var outcome = campaign?.Status == Domain.Advertising.Enums.AdCampaignStatus.Active
                ? AdvertisingFactOutcome.BudgetExceeded
                : AdvertisingFactOutcome.CampaignNotActive;
            return new AdvertisingFactRecordResult(outcome, null);
        }

        var impression = AdvertisingImpression.Record(campaignId, placementId, idempotencyKey, operationalCost, occurredAtUtc, utcNow);
        _impressions[impression.Id] = impression;
        return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Created, impression.Id);
    }
}
