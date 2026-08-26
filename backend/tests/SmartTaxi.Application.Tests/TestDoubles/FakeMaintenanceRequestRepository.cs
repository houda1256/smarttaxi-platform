using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of MaintenanceRequestRepository's atomic conditional TryTransitionAsync — same "reflection SetProperty helper" convention as FakeAdCampaignRepository, since MaintenanceRequest deliberately exposes no public status-mutation method.</summary>
public sealed class FakeMaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private static readonly MaintenanceRequestStatus[] TerminalStatuses =
    [
        MaintenanceRequestStatus.Completed, MaintenanceRequestStatus.Cancelled, MaintenanceRequestStatus.Rejected,
        MaintenanceRequestStatus.QuoteRejected
    ];

    private readonly Dictionary<Guid, MaintenanceRequest> _requests = new();

    public Task<bool> TryAddAsync(MaintenanceRequest request, CancellationToken cancellationToken)
    {
        if (_requests.Values.Any(r => r.VehicleId == request.VehicleId && !TerminalStatuses.Contains(r.Status)))
        {
            return Task.FromResult(false);
        }

        _requests[request.Id] = request;
        return Task.FromResult(true);
    }

    public Task<MaintenanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.GetValueOrDefault(requestId));

    public Task<PagedResult<MaintenanceRequest>> GetForOwnerAsync(Guid ownerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _requests.Values.Where(r => r.OwnerUserId == ownerUserId).ToList();
        return Task.FromResult(new PagedResult<MaintenanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<MaintenanceRequest>> GetForGarageAsync(Guid garageUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _requests.Values.Where(r => r.GarageUserId == garageUserId).ToList();
        return Task.FromResult(new PagedResult<MaintenanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<MaintenanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _requests.Values.ToList();
        return Task.FromResult(new PagedResult<MaintenanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<MaintenanceRequest>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MaintenanceRequest> items = _requests.Values.Where(r => r.VehicleId == vehicleId).ToList();
        return Task.FromResult(items);
    }

    public Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.Values.Any(r => r.VehicleId == vehicleId && !TerminalStatuses.Contains(r.Status)));

    public Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<MaintenanceRequestStatus> allowedFromStatuses, MaintenanceRequestStatus newStatus,
        Guid? requiredGarageUserId, Guid? requiredOwnerUserId, decimal? estimatedCost, decimal? finalCost, string? reason,
        Guid? cancelledByUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || !allowedFromStatuses.Contains(request.Status)
            || (requiredGarageUserId is not null && request.GarageUserId != requiredGarageUserId)
            || (requiredOwnerUserId is not null && request.OwnerUserId != requiredOwnerUserId))
        {
            return Task.FromResult(false);
        }

        var previousStatus = request.Status;

        SetProperty(request, nameof(MaintenanceRequest.Status), newStatus);
        SetProperty(request, nameof(MaintenanceRequest.UpdatedAtUtc), utcNow);

        if (newStatus == MaintenanceRequestStatus.QuotePending)
        {
            SetProperty(request, nameof(MaintenanceRequest.ConfirmedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.Rejected)
        {
            SetProperty(request, nameof(MaintenanceRequest.GarageRejectionReason), reason);
        }

        if (newStatus == MaintenanceRequestStatus.QuoteSubmitted)
        {
            SetProperty(request, nameof(MaintenanceRequest.EstimatedCost), estimatedCost);
            SetProperty(request, nameof(MaintenanceRequest.QuoteSubmittedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.QuoteAccepted)
        {
            SetProperty(request, nameof(MaintenanceRequest.QuoteAcceptedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.VehicleReceived)
        {
            SetProperty(request, nameof(MaintenanceRequest.VehicleReceivedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.InProgress && previousStatus == MaintenanceRequestStatus.VehicleReceived)
        {
            SetProperty(request, nameof(MaintenanceRequest.WorkStartedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.Completed)
        {
            SetProperty(request, nameof(MaintenanceRequest.FinalCost), finalCost);
            SetProperty(request, nameof(MaintenanceRequest.CompletedAtUtc), utcNow);
        }

        if (newStatus == MaintenanceRequestStatus.Cancelled)
        {
            SetProperty(request, nameof(MaintenanceRequest.CancelledAtUtc), utcNow);
            SetProperty(request, nameof(MaintenanceRequest.CancellationReason), reason);
            SetProperty(request, nameof(MaintenanceRequest.CancelledByUserId), cancelledByUserId);
        }

        return Task.FromResult(true);
    }

    public Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || request.SettledAtUtc is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(MaintenanceRequest.SettledAtUtc), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(MaintenanceRequest request, string propertyName, object? value) =>
        typeof(MaintenanceRequest).GetProperty(propertyName)!.SetValue(request, value);
}
