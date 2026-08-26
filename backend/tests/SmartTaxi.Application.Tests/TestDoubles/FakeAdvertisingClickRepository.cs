using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdvertisingClickRepository : IAdvertisingClickRepository
{
    private readonly Dictionary<Guid, AdvertisingClick> _clicks = new();
    private readonly FakeAdCampaignRepository _campaignRepository;

    public FakeAdvertisingClickRepository(FakeAdCampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_clicks.Values.Count(c => c.CampaignId == campaignId));

    public async Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, Guid? impressionId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        var existing = _clicks.Values.FirstOrDefault(c => c.IdempotencyKey == idempotencyKey);

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

        var click = AdvertisingClick.Record(campaignId, placementId, impressionId, idempotencyKey, operationalCost, occurredAtUtc, utcNow);
        _clicks[click.Id] = click;
        return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Created, click.Id);
    }
}
