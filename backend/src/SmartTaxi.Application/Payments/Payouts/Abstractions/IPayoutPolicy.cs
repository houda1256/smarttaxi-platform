namespace SmartTaxi.Application.Payments.Payouts.Abstractions;

/// <summary>Configurable, like IPlatformCommissionPolicy — no payout below this amount may be requested (avoids issuing near-zero bank transfers).</summary>
public interface IPayoutPolicy
{
    decimal MinimumPayoutAmount { get; }
}
