namespace SmartTaxi.Domain.Rides.Enums;

/// <summary>
/// Modeled as a single flat dimension per the master prompt's own framing
/// ("Support: Immediate, Scheduled, Negotiated, Shared"), even though timing
/// (Immediate/Scheduled) and fare/pooling model (Negotiated/Shared) are
/// conceptually different axes — kept literal to the spec rather than
/// introducing an orthogonal-flags design that wasn't asked for.
/// </summary>
public enum RideType
{
    Immediate,
    Scheduled,
    Negotiated,
    Shared
}
