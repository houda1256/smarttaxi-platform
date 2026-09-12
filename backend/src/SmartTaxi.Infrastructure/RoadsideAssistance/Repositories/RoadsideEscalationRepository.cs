using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

/// <summary>
/// Mandatory atomicity design (approved plan, Q4): one transaction guards
/// RoadsideAssistanceRequest.EscalatedMaintenanceRequestId IS NULL AND
/// Status == Completed, creates the MaintenanceRequest via Module 9's own
/// public seam (MaintenanceRequest.Create + IMaintenanceRequestRepository.TryAddAsync
/// — never re-implemented, never a MaintenanceRecord write, never a garage
/// auto-selection, never any other Maintenance table touched), and sets
/// EscalatedMaintenanceRequestId in the same transaction. If Maintenance's own
/// one-active-request-per-vehicle partial unique index rejects the insert,
/// the whole transaction rolls back and EscalatedMaintenanceRequestId stays
/// null — a retry is then safe.
/// </summary>
internal sealed class RoadsideEscalationRepository : IRoadsideEscalationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMaintenanceRequestRepository _maintenanceRequestRepository;

    public RoadsideEscalationRepository(ApplicationDbContext context, IMaintenanceRequestRepository maintenanceRequestRepository)
    {
        _context = context;
        _maintenanceRequestRepository = maintenanceRequestRepository;
    }

    public async Task<RoadsideEscalationResult> TryEscalateAsync(
        Guid roadsideRequestId, Guid ownerUserId, Guid garageUserId, string description, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var request = await _context.RoadsideAssistanceRequests.FirstOrDefaultAsync(r => r.Id == roadsideRequestId, cancellationToken);

        if (request is null || request.Status != RoadsideRequestStatus.Completed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.RequestNotEligible, null);
        }

        if (request.EscalatedMaintenanceRequestId is { } existingId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.AlreadyEscalated, existingId);
        }

        var maintenanceRequest = MaintenanceRequest.Create(request.VehicleId, ownerUserId, garageUserId, description, utcNow);

        if (!await _maintenanceRequestRepository.TryAddAsync(maintenanceRequest, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.MaintenanceConflict, null);
        }

        var rows = await _context.RoadsideAssistanceRequests
            .Where(r => r.Id == roadsideRequestId && r.EscalatedMaintenanceRequestId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.EscalatedMaintenanceRequestId, maintenanceRequest.Id)
                .SetProperty(r => r.UpdatedAtUtc, utcNow), cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.RequestNotEligible, null);
        }

        await transaction.CommitAsync(cancellationToken);
        return new RoadsideEscalationResult(RoadsideEscalationOutcome.Escalated, maintenanceRequest.Id);
    }
}
