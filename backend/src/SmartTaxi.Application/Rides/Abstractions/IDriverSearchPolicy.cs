namespace SmartTaxi.Application.Rides.Abstractions;

public interface IDriverSearchPolicy
{
    /// <summary>Tried in order; the first radius yielding at least one eligible Driver is used.</summary>
    IReadOnlyList<int> ProgressiveSearchRadiusKm { get; }

    int DriverResponseTimeoutSeconds { get; }
}
