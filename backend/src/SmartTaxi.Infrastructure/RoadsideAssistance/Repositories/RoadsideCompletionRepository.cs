using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

/// <summary>
/// Mandatory atomicity design (approved plan): one transaction contains the
/// RoadsideAssistanceRequest -&gt; Completed transition and a best-effort
/// attempt to release the vehicle from UnderRoadsideAssistance back to
/// Active. The release's result deliberately does NOT gate the transaction's
/// success — see IRoadsideCompletionRepository's own doc comment: a vehicle
/// that's no longer UnderRoadsideAssistance for a legitimate independent
/// reason (e.g. Suspended) must never be silently reactivated, and completion
/// must still succeed regardless, because the intervention itself genuinely
/// finished. Used ONLY for immobilizing RoadsideServiceType values.
/// </summary>
internal sealed class RoadsideCompletionRepository : IRoadsideCompletionRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.InProgress];

    private readonly ApplicationDbContext _context;
    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public RoadsideCompletionRepository(
        ApplicationDbContext context, IRoadsideAssistanceRequestRepository requestRepository, IVehicleRepository vehicleRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<RoadsideCompletionResult> TryCompleteAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, decimal finalCost, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.Completed, requiredRequesterUserId: null, requiredPartnerUserId: partnerUserId,
            finalCost, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RoadsideCompletionResult.RequestNotEligible;
        }

        // Best-effort, unconditionally attempted, never gates the transaction — see the explicit business
        // rule in this method's doc comment. A `false` result means the vehicle was already not
        // UnderRoadsideAssistance for another legitimate reason, which is the correct outcome, not an error.
        await _vehicleRepository.TryReleaseFromRoadsideAssistanceAsync(vehicleId, utcNow, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return RoadsideCompletionResult.Completed;
    }
}
