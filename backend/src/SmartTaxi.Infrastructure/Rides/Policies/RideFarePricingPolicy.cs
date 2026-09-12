using Microsoft.Extensions.Options;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Infrastructure.Rides.Options;

namespace SmartTaxi.Infrastructure.Rides.Policies;

internal sealed class RideFarePricingPolicy : IFarePricingPolicy
{
    private readonly RidePricingOptions _options;

    public RideFarePricingPolicy(IOptions<RidePricingOptions> options)
    {
        _options = options.Value;
    }

    public decimal BaseFare => _options.BaseFare;
    public decimal PricePerKilometer => _options.PricePerKilometer;
    public decimal PricePerMinute => _options.PricePerMinute;
    public decimal BookingFee => _options.BookingFee;
    public decimal MinimumFare => _options.MinimumFare;
    public decimal WaitingFeePerMinute => _options.WaitingFeePerMinute;
    public decimal CancellationFeeAfterAcceptance => _options.CancellationFeeAfterAcceptance;
    public decimal CancellationFeeAfterArrival => _options.CancellationFeeAfterArrival;

    public decimal GetCategoryAdjustment(VehicleCategory category) =>
        _options.CategoryAdjustments.GetValueOrDefault(category.ToString(), 0m);
}
