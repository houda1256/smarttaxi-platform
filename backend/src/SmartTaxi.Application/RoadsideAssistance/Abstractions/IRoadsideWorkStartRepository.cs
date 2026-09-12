using SmartTaxi.Application.RoadsideAssistance.Contracts;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan): ONE database transaction
/// guarantees RoadsideAssistanceRequest.Status == InProgress AND
/// Vehicle.OperationalStatus == UnderRoadsideAssistance after commit, or
/// neither changed — never two independently committed operations. Used ONLY
/// when RoadsideServiceTypePolicy.RequiresVehicleImmobilization is true; for
/// non-immobilizing service types the plain IRoadsideAssistanceRequestRepository.TryTransitionAsync
/// is used instead and Fleet is never called.
/// </summary>
public interface IRoadsideWorkStartRepository
{
    Task<RoadsideWorkStartResult> TryStartAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);
}
