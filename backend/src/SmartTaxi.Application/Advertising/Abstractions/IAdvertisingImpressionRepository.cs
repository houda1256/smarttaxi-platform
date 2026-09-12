using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdvertisingImpressionRepository
{
    Task<AdvertisingImpression?> GetByIdAsync(Guid impressionId, CancellationToken cancellationToken);

    Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically: checks the IdempotencyKey, consumes the campaign's
    /// operational budget (conditional on the campaign being Active and both
    /// total and daily remaining budget absorbing the cost), and inserts the
    /// impression row — all as one transaction, so two concurrent impressions
    /// can never together exceed the budget and a duplicate event can never
    /// double-consume it.
    /// </summary>
    Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc, DateTime utcNow,
        CancellationToken cancellationToken);
}
