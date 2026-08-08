namespace SmartTaxi.Application.Payments.Abstractions;

/// <summary>Deterministic, explainable revenue split — never ML, never payroll.</summary>
public interface IRevenueSharingCalculator
{
    Task<RevenueShareResult> CalculateAsync(Guid driverId, Guid ownerId, decimal fareAmount, CancellationToken cancellationToken);
}

public sealed record RevenueShareResult(decimal DriverAmount, decimal OwnerAmount, decimal PlatformCommissionAmount, string Explanation);
