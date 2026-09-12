using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of RoadsideEscalationRepository's cross-module atomicity — composes the SAME FakeRoadsideAssistanceRequestRepository/FakeMaintenanceRequestRepository instances a test also uses, reusing Module 9's own public TryAddAsync seam exactly like the real Infrastructure implementation.</summary>
public sealed class FakeRoadsideEscalationRepository : IRoadsideEscalationRepository
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository;
    private readonly FakeMaintenanceRequestRepository _maintenanceRequestRepository;

    public FakeRoadsideEscalationRepository(
        FakeRoadsideAssistanceRequestRepository requestRepository, FakeMaintenanceRequestRepository maintenanceRequestRepository)
    {
        _requestRepository = requestRepository;
        _maintenanceRequestRepository = maintenanceRequestRepository;
    }

    public async Task<RoadsideEscalationResult> TryEscalateAsync(
        Guid roadsideRequestId, Guid ownerUserId, Guid garageUserId, string description, DateTime utcNow, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(roadsideRequestId, cancellationToken);

        if (request is null || request.Status != RoadsideRequestStatus.Completed)
        {
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.RequestNotEligible, null);
        }

        if (request.EscalatedMaintenanceRequestId is { } existingId)
        {
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.AlreadyEscalated, existingId);
        }

        var maintenanceRequest = MaintenanceRequest.Create(request.VehicleId, ownerUserId, garageUserId, description, utcNow);

        if (!await _maintenanceRequestRepository.TryAddAsync(maintenanceRequest, cancellationToken))
        {
            return new RoadsideEscalationResult(RoadsideEscalationOutcome.MaintenanceConflict, null);
        }

        await _requestRepository.TrySetEscalatedMaintenanceRequestIdAsync(roadsideRequestId, maintenanceRequest.Id, utcNow, cancellationToken);

        return new RoadsideEscalationResult(RoadsideEscalationOutcome.Escalated, maintenanceRequest.Id);
    }
}
