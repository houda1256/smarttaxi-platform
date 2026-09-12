using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFarePricingPolicy : IFarePricingPolicy
{
    public decimal BaseFare { get; init; } = 2m;
    public decimal PricePerKilometer { get; init; } = 0.8m;
    public decimal PricePerMinute { get; init; } = 0.1m;
    public decimal BookingFee { get; init; } = 1m;
    public decimal MinimumFare { get; init; } = 5m;
    public decimal WaitingFeePerMinute { get; init; } = 0.2m;
    public decimal CancellationFeeAfterAcceptance { get; init; } = 3m;
    public decimal CancellationFeeAfterArrival { get; init; } = 5m;

    public decimal GetCategoryAdjustment(VehicleCategory category) => 0m;
}
