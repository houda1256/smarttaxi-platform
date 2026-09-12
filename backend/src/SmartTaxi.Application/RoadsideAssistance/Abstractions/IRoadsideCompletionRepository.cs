using SmartTaxi.Application.RoadsideAssistance.Contracts;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan): ONE database transaction
/// contains the RoadsideAssistanceRequest -&gt; Completed transition and a
/// best-effort attempt to release the vehicle from UnderRoadsideAssistance
/// back to Active. The release attempt deliberately does NOT gate the
/// transaction's success: if the vehicle is no longer
/// UnderRoadsideAssistance (e.g. independently Suspended by an admin
/// mid-intervention), that is the correct, expected outcome — "no
/// reactivation required" — not a failure. The intervention itself genuinely
/// finished and must still be recorded regardless of the vehicle's
/// independent state. Used ONLY for immobilizing service types.
/// </summary>
public interface IRoadsideCompletionRepository
{
    Task<RoadsideCompletionResult> TryCompleteAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, decimal finalCost, DateTime utcNow, CancellationToken cancellationToken);
}
