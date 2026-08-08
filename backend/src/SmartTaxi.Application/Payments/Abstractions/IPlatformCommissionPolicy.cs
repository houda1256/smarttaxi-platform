namespace SmartTaxi.Application.Payments.Abstractions;

/// <summary>Configurable, never hardcoded — development seed value bound from configuration (default 10%).</summary>
public interface IPlatformCommissionPolicy
{
    decimal CommissionPercentage { get; }
}
