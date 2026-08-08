using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IFareCalculator
{
    FareBreakdown Calculate(FareCalculationInput input);
}

public sealed record FareCalculationInput(
    decimal DistanceKm,
    int DurationMinutes,
    VehicleCategory VehicleCategory,
    decimal DynamicMultiplier,
    int WaitingMinutes = 0,
    decimal PromotionDiscount = 0,
    decimal SubscriptionAdjustment = 0);

public sealed record FareBreakdown(
    decimal BaseFare,
    decimal DistanceFare,
    decimal DurationFare,
    decimal BookingFee,
    decimal CategoryAdjustment,
    decimal DynamicPricingAmount,
    decimal WaitingFee,
    decimal PromotionDiscount,
    decimal SubscriptionAdjustment,
    decimal TotalFare);
