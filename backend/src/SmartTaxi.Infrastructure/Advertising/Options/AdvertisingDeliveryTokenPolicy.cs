using Microsoft.Extensions.Options;
using SmartTaxi.Application.Advertising.Abstractions;

namespace SmartTaxi.Infrastructure.Advertising.Options;

internal sealed class AdvertisingDeliveryTokenPolicy : IAdvertisingDeliveryTokenPolicy
{
    private readonly AdvertisingOptions _options;

    public AdvertisingDeliveryTokenPolicy(IOptions<AdvertisingOptions> options)
    {
        _options = options.Value;
    }

    public TimeSpan TokenLifetime => TimeSpan.FromSeconds(_options.DeliveryTokenLifetimeSeconds);
}
