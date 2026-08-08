using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSharedRideMatchingService : ISharedRideMatchingService
{
    public bool IsCompatible { get; set; } = true;

    public SharedRideCompatibilityReport CheckCompatibility(SharedRideCompatibilityInput input) =>
        new(IsCompatible, IsCompatible ? [] : ["Incompatible (fake)"]);
}
