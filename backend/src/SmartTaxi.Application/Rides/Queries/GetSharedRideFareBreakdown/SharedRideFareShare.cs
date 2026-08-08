namespace SmartTaxi.Application.Rides.Queries.GetSharedRideFareBreakdown;

/// <summary>
/// Transparent per-participant breakdown: the common-route portion and the
/// booking fee are split evenly between the two paired Rides; each
/// participant additionally bears their own personal-detour distance/time
/// beyond the shared portion. No Payment transaction is created — this is
/// purely an estimate shown before either Customer accepts.
/// </summary>
public sealed record SharedRideFareShare(
    Guid RideId, Guid CustomerId, decimal CommonRouteShare, decimal PersonalDetourFare, decimal BookingFeeShare, decimal TotalEstimatedFare);
