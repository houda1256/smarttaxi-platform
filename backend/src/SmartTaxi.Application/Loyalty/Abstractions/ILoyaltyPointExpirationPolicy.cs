namespace SmartTaxi.Application.Loyalty.Abstractions;

/// <summary>Admin-configurable via IOptions&lt;T&gt; — mirrors the spec's Loyalty.PointExpirationMonths setting name. ExpirationMonths of 0 or less means RewardPoints never expire.</summary>
public interface ILoyaltyPointExpirationPolicy
{
    int ExpirationMonths { get; }
}
