using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdvertisingClickRepository
{
    Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>Same atomicity guarantee as IAdvertisingImpressionRepository.TryRecordAsync, applied to clicks (relevant when PricingModel is Cpc).</summary>
    Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, Guid? impressionId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc,
        DateTime utcNow, CancellationToken cancellationToken);
}
