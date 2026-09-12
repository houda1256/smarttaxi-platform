namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>Configurable staleness threshold for the manual expire-sweep — same "policy interface, no scheduler" convention as IDriverSearchPolicy.</summary>
public interface IRoadsideExpiryPolicy
{
    TimeSpan StaleAfter { get; }
}
