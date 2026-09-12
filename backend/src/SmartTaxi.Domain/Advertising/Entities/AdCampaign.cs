using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Advertising.Events;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>
/// Status transitions (Submit/Approve/Reject/RequestChanges/Schedule/
/// Activate/Pause/Resume/Suspend/Reactivate/Complete/Cancel) are deliberately
/// NOT modeled as mutating methods here — they are enforced as atomic,
/// race-safe conditional SQL updates in the repository (same convention as
/// ProfessionalAccountRequest/RideRepository/SubscriptionRepository), so the
/// guard condition has exactly one source of truth. This entity only owns
/// field-level construction/edit validation and the pure "is this change
/// material" rule, which is genuine domain logic independent of concurrency
/// concerns. Targeting fields are a stable snapshot on the campaign itself
/// (never a live external lookup) so an approved campaign's targeting can
/// never silently drift — see the Module 8 audit's targeting-snapshot
/// requirement. Money fields are decimal only, never float/double.
/// </summary>
public sealed class AdCampaign : AggregateRoot
{
    public Guid AdvertiserProfileId { get; private set; }
    public Guid AdvertiserUserId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public Guid PlacementId { get; private set; }
    public DateTime StartAtUtc { get; private set; }
    public DateTime EndAtUtc { get; private set; }
    public AdCampaignStatus Status { get; private set; }
    public AdPricingModel PricingModel { get; private set; }

    /// <summary>Meaning depends on PricingModel: total price for Flat/Duration, price per 1000 impressions for Cpm, price per click for Cpc.</summary>
    public decimal PriceRate { get; private set; }

    public decimal BudgetLimit { get; private set; }
    public decimal ConsumedBudget { get; private set; }
    public decimal? DailyBudgetLimit { get; private set; }

    /// <summary>Rolling daily counter — reset (by the repository's atomic guard) whenever DailyConsumedDateUtc no longer matches the current UTC date. Both fields are operational bookkeeping only, mutated exclusively via the repository's atomic conditional UPDATE, never a domain method.</summary>
    public decimal DailyConsumedBudget { get; private set; }
    public DateTime? DailyConsumedDateUtc { get; private set; }

    /// <summary>Free-text, normalized (trimmed/uppercased) — the audit found no controlled Zone/Geography catalog; documented as a deliberate interim limitation, not a real zone.</summary>
    public string? TargetCity { get; private set; }
    public string? TargetVehicleCategory { get; private set; }
    public string? TargetDaysOfWeek { get; private set; }
    public int? TargetStartHour { get; private set; }
    public int? TargetEndHour { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }

    private AdCampaign()
    {
    }

    private AdCampaign(
        Guid advertiserProfileId, Guid advertiserUserId, string name, string description, string objective, Guid placementId,
        DateTime startAtUtc, DateTime endAtUtc, AdPricingModel pricingModel, decimal priceRate, decimal budgetLimit,
        decimal? dailyBudgetLimit, string? targetCity, string? targetVehicleCategory, string? targetDaysOfWeek, int? targetStartHour,
        int? targetEndHour, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        AdvertiserProfileId = advertiserProfileId;
        AdvertiserUserId = advertiserUserId;
        Code = GenerateCode(utcNow);
        Name = name;
        Description = description;
        Objective = objective;
        PlacementId = placementId;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        Status = AdCampaignStatus.Draft;
        PricingModel = pricingModel;
        PriceRate = priceRate;
        BudgetLimit = budgetLimit;
        DailyBudgetLimit = dailyBudgetLimit;
        TargetCity = NormalizeCity(targetCity);
        TargetVehicleCategory = targetVehicleCategory?.Trim();
        TargetDaysOfWeek = targetDaysOfWeek?.Trim();
        TargetStartHour = targetStartHour;
        TargetEndHour = targetEndHour;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new AdCampaignCreated(Id, advertiserUserId, utcNow));
    }

    public static AdCampaign Create(
        Guid advertiserProfileId, Guid advertiserUserId, string name, string description, string objective, Guid placementId,
        DateTime startAtUtc, DateTime endAtUtc, AdPricingModel pricingModel, decimal priceRate, decimal budgetLimit,
        decimal? dailyBudgetLimit, string? targetCity, string? targetVehicleCategory, string? targetDaysOfWeek, int? targetStartHour,
        int? targetEndHour, DateTime utcNow)
    {
        ValidateFields(name, startAtUtc, endAtUtc, priceRate, budgetLimit, dailyBudgetLimit, targetStartHour, targetEndHour);

        return new AdCampaign(
            advertiserProfileId, advertiserUserId, name.Trim(), (description ?? string.Empty).Trim(), (objective ?? string.Empty).Trim(),
            placementId, startAtUtc, endAtUtc, pricingModel, priceRate, budgetLimit, dailyBudgetLimit, targetCity, targetVehicleCategory,
            targetDaysOfWeek, targetStartHour, targetEndHour, utcNow);
    }

    /// <summary>Used both for a Draft's free edit and for an Approved/Scheduled/Active campaign's material edit — the caller decides, via WouldBeMaterialChange below, whether the resulting update must also force the campaign back for re-review.</summary>
    public void UpdateFields(
        string name, string description, string objective, Guid placementId, DateTime startAtUtc, DateTime endAtUtc,
        AdPricingModel pricingModel, decimal priceRate, decimal budgetLimit, decimal? dailyBudgetLimit, string? targetCity,
        string? targetVehicleCategory, string? targetDaysOfWeek, int? targetStartHour, int? targetEndHour, DateTime utcNow)
    {
        ValidateFields(name, startAtUtc, endAtUtc, priceRate, budgetLimit, dailyBudgetLimit, targetStartHour, targetEndHour);

        Name = name.Trim();
        Description = (description ?? string.Empty).Trim();
        Objective = (objective ?? string.Empty).Trim();
        PlacementId = placementId;
        StartAtUtc = startAtUtc;
        EndAtUtc = endAtUtc;
        PricingModel = pricingModel;
        PriceRate = priceRate;
        BudgetLimit = budgetLimit;
        DailyBudgetLimit = dailyBudgetLimit;
        TargetCity = NormalizeCity(targetCity);
        TargetVehicleCategory = targetVehicleCategory?.Trim();
        TargetDaysOfWeek = targetDaysOfWeek?.Trim();
        TargetStartHour = targetStartHour;
        TargetEndHour = targetEndHour;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Material fields per the Module 8 audit: creative/placement, targeting, dates, budget, and
    /// pricing. A change to Name/Description/Objective alone is never material (cosmetic only).
    /// </summary>
    public bool WouldBeMaterialChange(
        Guid placementId, DateTime startAtUtc, DateTime endAtUtc, AdPricingModel pricingModel, decimal priceRate, decimal budgetLimit,
        decimal? dailyBudgetLimit, string? targetCity, string? targetVehicleCategory, string? targetDaysOfWeek, int? targetStartHour,
        int? targetEndHour) =>
        PlacementId != placementId
        || StartAtUtc != startAtUtc
        || EndAtUtc != endAtUtc
        || PricingModel != pricingModel
        || PriceRate != priceRate
        || BudgetLimit != budgetLimit
        || DailyBudgetLimit != dailyBudgetLimit
        || TargetCity != NormalizeCity(targetCity)
        || TargetVehicleCategory != targetVehicleCategory?.Trim()
        || TargetDaysOfWeek != targetDaysOfWeek?.Trim()
        || TargetStartHour != targetStartHour
        || TargetEndHour != targetEndHour;

    public decimal RemainingBudget => BudgetLimit - ConsumedBudget;

    private static void ValidateFields(
        string name, DateTime startAtUtc, DateTime endAtUtc, decimal priceRate, decimal budgetLimit, decimal? dailyBudgetLimit,
        int? targetStartHour, int? targetEndHour)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de la campagne est requis.");
        }

        if (endAtUtc <= startAtUtc)
        {
            throw new ArgumentException("La date de fin doit être postérieure à la date de début.");
        }

        if (priceRate < 0)
        {
            throw new ArgumentException("Le tarif ne peut pas être négatif.");
        }

        if (budgetLimit < 0)
        {
            throw new ArgumentException("Le budget total ne peut pas être négatif.");
        }

        if (dailyBudgetLimit is < 0)
        {
            throw new ArgumentException("Le budget journalier ne peut pas être négatif.");
        }

        if (dailyBudgetLimit > budgetLimit)
        {
            throw new ArgumentException("Le budget journalier ne peut pas dépasser le budget total.");
        }

        if (targetStartHour is < 0 or > 23 || targetEndHour is < 0 or > 23)
        {
            throw new ArgumentException("Les heures de ciblage doivent être comprises entre 0 et 23.");
        }
    }

    private static string? NormalizeCity(string? city) => string.IsNullOrWhiteSpace(city) ? null : city.Trim().ToUpperInvariant();

    private static string GenerateCode(DateTime utcNow) => $"CAMP-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
