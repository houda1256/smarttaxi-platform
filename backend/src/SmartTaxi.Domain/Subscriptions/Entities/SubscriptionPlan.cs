using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Domain.Subscriptions.Entities;

/// <summary>
/// An offer available to a given actor role (Customer, Driver, TaxiOwner,
/// GaragePartner, RoadsideAssistancePartner, Advertiser, BusinessCustomer —
/// reuses Identity.UserRole rather than a second role enum). IsActive
/// toggling is an atomic repository-level guard (mirrors TaxRule's own
/// Deactivate); everything else here is a plain field edit, so UpdateDetails
/// is a normal domain method, same split as TaxiOwnerProfile.
/// </summary>
public sealed class SubscriptionPlan : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public UserRole TargetRole { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BillingPeriod BillingPeriod { get; private set; }
    public int TrialPeriodDays { get; private set; }
    public IReadOnlyList<string> Features { get; private set; } = [];
    public IReadOnlyDictionary<string, int> Limits { get; private set; } = new Dictionary<string, int>();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SubscriptionPlan()
    {
    }

    private SubscriptionPlan(
        string name, string code, string description, UserRole targetRole, decimal price, string currency,
        BillingPeriod billingPeriod, int trialPeriodDays, IReadOnlyList<string> features,
        IReadOnlyDictionary<string, int> limits, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Name = name;
        Code = code;
        Description = description;
        TargetRole = targetRole;
        Price = price;
        Currency = currency;
        BillingPeriod = billingPeriod;
        TrialPeriodDays = trialPeriodDays;
        Features = features;
        Limits = limits;
        IsActive = true;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static SubscriptionPlan Create(
        string name, string code, string description, UserRole targetRole, decimal price, string currency,
        BillingPeriod billingPeriod, int trialPeriodDays, IReadOnlyList<string> features,
        IReadOnlyDictionary<string, int> limits, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom du plan est requis.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du plan est requis.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Le prix ne peut pas être négatif.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        if (trialPeriodDays < 0)
        {
            throw new ArgumentException("La période d'essai ne peut pas être négative.");
        }

        return new SubscriptionPlan(
            name.Trim(), code.Trim().ToUpperInvariant(), description.Trim(), targetRole, price,
            currency.Trim().ToUpperInvariant(), billingPeriod, trialPeriodDays, features, limits, utcNow);
    }

    public void UpdateDetails(
        string name, string description, decimal price, IReadOnlyList<string> features,
        IReadOnlyDictionary<string, int> limits, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom du plan est requis.");
        }

        if (price < 0)
        {
            throw new ArgumentException("Le prix ne peut pas être négatif.");
        }

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Features = features;
        Limits = limits;
        UpdatedAt = utcNow;
    }
}
