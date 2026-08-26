using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Policies;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

/// <summary>
/// Admin escape hatch — allowed from any non-terminal status (see
/// RoadsideAssistanceRequestRepository.TerminalStatuses). One transaction: the
/// RoadsideAssistanceRequest -&gt; Cancelled transition (no partner/requester
/// requirement — this is an admin action), then — ONLY when the request's
/// ServiceType requires vehicle immobilization AND the current status is
/// InProgress (the only case Fleet could actually have been touched) — a
/// best-effort, non-gating Fleet release. Non-immobilizing service types and
/// any pre-InProgress status never call Fleet at all.
/// </summary>
internal sealed class RoadsideForceCancelRepository : IRoadsideForceCancelRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses =
        Enum.GetValues<RoadsideRequestStatus>()
            .Except(RoadsideAssistanceRequestRepository.TerminalStatuses.Append(RoadsideRequestStatus.Requested))
            .ToArray();

    private readonly ApplicationDbContext _context;
    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public RoadsideForceCancelRepository(
        ApplicationDbContext context, IRoadsideAssistanceRequestRepository requestRepository, IVehicleRepository vehicleRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<bool> TryForceCancelAsync(Guid requestId, Guid adminUserId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var request = await _context.RoadsideAssistanceRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var shouldReleaseVehicle = request.Status == RoadsideRequestStatus.InProgress
            && RoadsideServiceTypePolicy.RequiresVehicleImmobilization(request.ServiceType);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.Cancelled, requiredRequesterUserId: null, requiredPartnerUserId: null,
            finalCost: null, reason, cancelledByUserId: adminUserId, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (shouldReleaseVehicle)
        {
            // Best-effort, never gates the transaction — safe no-op if the vehicle already changed for
            // another legitimate reason (e.g. independently Suspended mid-intervention).
            await _vehicleRepository.TryReleaseFromRoadsideAssistanceAsync(request.VehicleId, utcNow, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
