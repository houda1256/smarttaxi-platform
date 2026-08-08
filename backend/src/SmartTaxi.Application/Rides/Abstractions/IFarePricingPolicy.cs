using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>Development seed values, bound from configuration — never hardcoded permanent production prices.</summary>
public interface IFarePricingPolicy
{
    decimal BaseFare { get; }
    decimal PricePerKilometer { get; }
    decimal PricePerMinute { get; }
    decimal BookingFee { get; }
    decimal MinimumFare { get; }
    decimal WaitingFeePerMinute { get; }
    decimal CancellationFeeAfterAcceptance { get; }
    decimal CancellationFeeAfterArrival { get; }

    decimal GetCategoryAdjustment(VehicleCategory category);
}
