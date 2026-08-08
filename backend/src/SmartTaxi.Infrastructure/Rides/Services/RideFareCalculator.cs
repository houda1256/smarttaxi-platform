using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>
/// Implements the documented fare formula: BaseFare + DistanceKm×PricePerKilometer
/// + DurationMinutes×PricePerMinute + BookingFee + CategoryAdjustment + WaitingFee,
/// then the dynamic multiplier is applied to that whole subtotal (its contribution
/// is reported separately as DynamicPricingAmount so it stays explainable), and
/// finally Promotion/Subscription adjustments are subtracted before the configured
/// MinimumFare floor is enforced.
/// </summary>
internal sealed class RideFareCalculator : IFareCalculator
{
    private readonly IFarePricingPolicy _pricingPolicy;

    public RideFareCalculator(IFarePricingPolicy pricingPolicy)
    {
        _pricingPolicy = pricingPolicy;
    }

    public FareBreakdown Calculate(FareCalculationInput input)
    {
        var baseFare = _pricingPolicy.BaseFare;
        var distanceFare = input.DistanceKm * _pricingPolicy.PricePerKilometer;
        var durationFare = input.DurationMinutes * _pricingPolicy.PricePerMinute;
        var bookingFee = _pricingPolicy.BookingFee;
        var categoryAdjustment = _pricingPolicy.GetCategoryAdjustment(input.VehicleCategory);
        var waitingFee = input.WaitingMinutes * _pricingPolicy.WaitingFeePerMinute;

        var subtotal = baseFare + distanceFare + durationFare + bookingFee + categoryAdjustment + waitingFee;
        var totalAfterMultiplier = subtotal * input.DynamicMultiplier;
        var dynamicPricingAmount = totalAfterMultiplier - subtotal;

        var totalBeforeFloor = totalAfterMultiplier - input.PromotionDiscount - input.SubscriptionAdjustment;
        var total = Math.Max(totalBeforeFloor, _pricingPolicy.MinimumFare);

        return new FareBreakdown(
            Math.Round(baseFare, 2), Math.Round(distanceFare, 2), Math.Round(durationFare, 2), Math.Round(bookingFee, 2),
            Math.Round(categoryAdjustment, 2), Math.Round(dynamicPricingAmount, 2), Math.Round(waitingFee, 2),
            Math.Round(input.PromotionDiscount, 2), Math.Round(input.SubscriptionAdjustment, 2), Math.Round(total, 2));
    }
}
