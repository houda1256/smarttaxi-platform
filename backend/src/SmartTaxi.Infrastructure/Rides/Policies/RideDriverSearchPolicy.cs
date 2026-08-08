using Microsoft.Extensions.Options;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Infrastructure.Rides.Options;

namespace SmartTaxi.Infrastructure.Rides.Policies;

internal sealed class RideDriverSearchPolicy : IDriverSearchPolicy
{
    public IReadOnlyList<int> ProgressiveSearchRadiusKm { get; }
    public int DriverResponseTimeoutSeconds { get; }

    public RideDriverSearchPolicy(IOptions<RideDriverSearchOptions> options)
    {
        var value = options.Value;
        ProgressiveSearchRadiusKm = value.ProgressiveSearchRadiusKm;
        DriverResponseTimeoutSeconds = value.DriverResponseTimeoutSeconds;
    }
}
