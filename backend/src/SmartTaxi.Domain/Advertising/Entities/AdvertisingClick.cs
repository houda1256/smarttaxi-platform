using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>Immutable, append-only fact — one row per click. ImpressionId is optional (a click is not always traceable to one specific recorded impression) but, when supplied, must belong to the same campaign — enforced by the Application handler, not here. No passenger/user identity is stored.</summary>
public sealed class AdvertisingClick : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid PlacementId { get; private set; }
    public Guid? ImpressionId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public decimal OperationalCost { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AdvertisingClick()
    {
    }

    private AdvertisingClick(
        Guid campaignId, Guid placementId, Guid? impressionId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc,
        DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CampaignId = campaignId;
        PlacementId = placementId;
        ImpressionId = impressionId;
        IdempotencyKey = idempotencyKey;
        OperationalCost = operationalCost;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = utcNow;
    }

    public static AdvertisingClick Record(
        Guid campaignId, Guid placementId, Guid? impressionId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Une clé d'idempotence est requise.");
        }

        if (operationalCost < 0)
        {
            throw new ArgumentException("Le coût opérationnel ne peut pas être négatif.");
        }

        return new AdvertisingClick(campaignId, placementId, impressionId, idempotencyKey.Trim(), operationalCost, occurredAtUtc, utcNow);
    }
}
