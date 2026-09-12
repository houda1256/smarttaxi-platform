using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>
/// Immutable, append-only fact — one row per actually-served ad impression.
/// IdempotencyKey is caller-supplied (there is no real ad-serving engine in
/// this digital-only phase; whatever surface displays the ad must generate
/// one key per real display event) and carries a DB unique index, which is
/// the actual anti-duplication/anti-fraud guarantee at the data layer. No
/// passenger/user identity is stored — only campaign/placement context.
/// </summary>
public sealed class AdvertisingImpression : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid PlacementId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public decimal OperationalCost { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AdvertisingImpression()
    {
    }

    private AdvertisingImpression(
        Guid campaignId, Guid placementId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CampaignId = campaignId;
        PlacementId = placementId;
        IdempotencyKey = idempotencyKey;
        OperationalCost = operationalCost;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = utcNow;
    }

    public static AdvertisingImpression Record(
        Guid campaignId, Guid placementId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Une clé d'idempotence est requise.");
        }

        if (operationalCost < 0)
        {
            throw new ArgumentException("Le coût opérationnel ne peut pas être négatif.");
        }

        return new AdvertisingImpression(campaignId, placementId, idempotencyKey.Trim(), operationalCost, occurredAtUtc, utcNow);
    }
}
