using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

public interface IRoadsidePartnerProfileRepository
{
    Task<RoadsidePartnerProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken);

    Task<RoadsidePartnerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Returns false (instead of throwing) if a concurrent registration already created a profile for this user — the unique index on UserId is the real enforcement point.</summary>
    Task<bool> TryAddAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(RoadsidePartnerProfile profile, CancellationToken cancellationToken);

    /// <summary>
    /// Compatibility pre-filter for partner recommendation (active + supports
    /// the service type + supports the vehicle category + same normalized city
    /// when a city is supplied) — the query handler applies distance scoring on
    /// top of this candidate set. Never a broadcast/browse endpoint: only ever
    /// called from the requester's own recommendation query.
    /// </summary>
    Task<IReadOnlyCollection<RoadsidePartnerProfile>> GetCompatibleCandidatesAsync(
        RoadsideServiceType serviceType, VehicleCategory vehicleCategory, string? city, CancellationToken cancellationToken);
}
