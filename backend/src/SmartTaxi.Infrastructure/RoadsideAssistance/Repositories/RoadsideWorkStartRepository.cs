using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

/// <summary>
/// Mandatory atomicity design (approved plan): composes IRoadsideAssistanceRequestRepository
/// (Roadside-side transition) and IVehicleRepository (Fleet-side transition)
/// — both injected with the SAME ApplicationDbContext instance (standard
/// DI-request scoping), so both ExecuteUpdateAsync calls join the ambient
/// transaction opened here and commit or roll back together. No compensation
/// logic: the rollback IS the compensation, guaranteed by the DB transaction
/// itself. Used ONLY for immobilizing RoadsideServiceType values.
/// </summary>
internal sealed class RoadsideWorkStartRepository : IRoadsideWorkStartRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.PartnerArrived];

    private readonly ApplicationDbContext _context;
    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public RoadsideWorkStartRepository(
        ApplicationDbContext context, IRoadsideAssistanceRequestRepository requestRepository, IVehicleRepository vehicleRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<RoadsideWorkStartResult> TryStartAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.InProgress, requiredRequesterUserId: null, requiredPartnerUserId: partnerUserId,
            finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RoadsideWorkStartResult.RequestNotEligible;
        }

        var vehicleMarked = await _vehicleRepository.TryMarkUnderRoadsideAssistanceAsync(vehicleId, utcNow, cancellationToken);

        if (!vehicleMarked)
        {
            // Rolls back the RoadsideAssistanceRequest transition above too — same transaction, same connection.
            await transaction.RollbackAsync(cancellationToken);
            return RoadsideWorkStartResult.VehicleNotEligible;
        }

        await transaction.CommitAsync(cancellationToken);
        return RoadsideWorkStartResult.Started;
    }
}
