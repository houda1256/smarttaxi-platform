using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of RoadsideAssistanceRequestRepository's atomic conditional updates — same "reflection SetProperty helper" convention as FakeMaintenanceRequestRepository, since RoadsideAssistanceRequest deliberately exposes no public status-mutation method.</summary>
public sealed class FakeRoadsideAssistanceRequestRepository : IRoadsideAssistanceRequestRepository
{
    private static readonly RoadsideRequestStatus[] TerminalStatuses =
    [
        RoadsideRequestStatus.Completed, RoadsideRequestStatus.Cancelled, RoadsideRequestStatus.Expired, RoadsideRequestStatus.Disputed
    ];

    private readonly Dictionary<Guid, RoadsideAssistanceRequest> _requests = new();
    private readonly List<RoadsidePartnerSelectionHistory> _history = [];

    public IReadOnlyCollection<RoadsidePartnerSelectionHistory> GetHistoryForTest(Guid requestId) =>
        _history.Where(h => h.RoadsideAssistanceRequestId == requestId).OrderBy(h => h.CycleNumber).ToList();

    public Task<bool> TryAddAsync(RoadsideAssistanceRequest request, CancellationToken cancellationToken)
    {
        if (_requests.Values.Any(r => r.VehicleId == request.VehicleId && !TerminalStatuses.Contains(r.Status)))
        {
            return Task.FromResult(false);
        }

        _requests[request.Id] = request;
        return Task.FromResult(true);
    }

    public Task<RoadsideAssistanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.GetValueOrDefault(requestId));

    public Task<PagedResult<RoadsideAssistanceRequest>> GetForRequesterAsync(
        Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _requests.Values.Where(r => r.RequesterUserId == requesterUserId).ToList();
        return Task.FromResult(new PagedResult<RoadsideAssistanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<RoadsideAssistanceRequest>> GetForPartnerAsync(
        Guid partnerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var requestIds = _history.Where(h => h.SelectedPartnerUserId == partnerUserId).Select(h => h.RoadsideAssistanceRequestId).ToHashSet();
        var items = _requests.Values.Where(r => requestIds.Contains(r.Id)).ToList();
        return Task.FromResult(new PagedResult<RoadsideAssistanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<RoadsideAssistanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _requests.Values.ToList();
        return Task.FromResult(new PagedResult<RoadsideAssistanceRequest>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        Task.FromResult(_requests.Values.Any(r => r.VehicleId == vehicleId && !TerminalStatuses.Contains(r.Status)));

    public Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<RoadsideRequestStatus> allowedFromStatuses, RoadsideRequestStatus newStatus,
        Guid? requiredRequesterUserId, Guid? requiredPartnerUserId, decimal? finalCost, string? reason, Guid? cancelledByUserId,
        bool clearSelectedPartner, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || !allowedFromStatuses.Contains(request.Status)
            || (requiredRequesterUserId is not null && request.RequesterUserId != requiredRequesterUserId)
            || (requiredPartnerUserId is not null && request.SelectedPartnerUserId != requiredPartnerUserId))
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(RoadsideAssistanceRequest.Status), newStatus);
        SetProperty(request, nameof(RoadsideAssistanceRequest.UpdatedAtUtc), utcNow);

        if (clearSelectedPartner)
        {
            SetProperty(request, nameof(RoadsideAssistanceRequest.SelectedPartnerUserId), null);
        }

        switch (newStatus)
        {
            case RoadsideRequestStatus.PartnerOnTheWay:
                SetProperty(request, nameof(RoadsideAssistanceRequest.PartnerOnTheWayAtUtc), utcNow);
                break;
            case RoadsideRequestStatus.PartnerArrived:
                SetProperty(request, nameof(RoadsideAssistanceRequest.PartnerArrivedAtUtc), utcNow);
                break;
            case RoadsideRequestStatus.InProgress:
                SetProperty(request, nameof(RoadsideAssistanceRequest.StartedAtUtc), utcNow);
                break;
            case RoadsideRequestStatus.Completed:
                SetProperty(request, nameof(RoadsideAssistanceRequest.FinalCost), finalCost);
                SetProperty(request, nameof(RoadsideAssistanceRequest.CompletedAtUtc), utcNow);
                break;
            case RoadsideRequestStatus.Cancelled:
                SetProperty(request, nameof(RoadsideAssistanceRequest.CancelledAtUtc), utcNow);
                SetProperty(request, nameof(RoadsideAssistanceRequest.CancellationReason), reason);
                SetProperty(request, nameof(RoadsideAssistanceRequest.CancelledByUserId), cancelledByUserId);
                break;
            case RoadsideRequestStatus.Expired:
                SetProperty(request, nameof(RoadsideAssistanceRequest.ExpiredAtUtc), utcNow);
                break;
            case RoadsideRequestStatus.Disputed:
                SetProperty(request, nameof(RoadsideAssistanceRequest.DisputedAtUtc), utcNow);
                break;
        }

        return Task.FromResult(true);
    }

    public Task<bool> TrySelectPartnerAsync(Guid requestId, Guid requesterUserId, Guid partnerUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || request.Status != RoadsideRequestStatus.PartnersAvailable
            || request.RequesterUserId != requesterUserId)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(RoadsideAssistanceRequest.Status), RoadsideRequestStatus.PendingPartnerResponse);
        SetProperty(request, nameof(RoadsideAssistanceRequest.SelectedPartnerUserId), partnerUserId);
        SetProperty(request, nameof(RoadsideAssistanceRequest.UpdatedAtUtc), utcNow);

        var cycleNumber = _history.Count(h => h.RoadsideAssistanceRequestId == requestId) + 1;
        _history.Add(new RoadsidePartnerSelectionHistory(requestId, cycleNumber, partnerUserId, utcNow));

        return Task.FromResult(true);
    }

    public Task<bool> TryRespondAsync(
        Guid requestId, Guid partnerUserId, RoadsidePartnerResponse response, decimal? estimatedCost, string? rejectionReason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || request.Status != RoadsideRequestStatus.PendingPartnerResponse
            || request.SelectedPartnerUserId != partnerUserId)
        {
            return Task.FromResult(false);
        }

        var pendingHistory = _history.FirstOrDefault(h => h.RoadsideAssistanceRequestId == requestId && h.Response is null);

        if (pendingHistory is null)
        {
            return Task.FromResult(false);
        }

        var newStatus = response == RoadsidePartnerResponse.Accepted ? RoadsideRequestStatus.Accepted : RoadsideRequestStatus.Rejected;
        SetProperty(request, nameof(RoadsideAssistanceRequest.Status), newStatus);
        SetProperty(request, nameof(RoadsideAssistanceRequest.UpdatedAtUtc), utcNow);

        if (response == RoadsidePartnerResponse.Accepted)
        {
            SetProperty(request, nameof(RoadsideAssistanceRequest.AcceptedAtUtc), utcNow);
            SetProperty(request, nameof(RoadsideAssistanceRequest.EstimatedCost), estimatedCost);
        }

        SetHistoryProperty(pendingHistory, nameof(RoadsidePartnerSelectionHistory.Response), response);
        SetHistoryProperty(pendingHistory, nameof(RoadsidePartnerSelectionHistory.RejectionReason), rejectionReason);
        SetHistoryProperty(pendingHistory, nameof(RoadsidePartnerSelectionHistory.RespondedAtUtc), utcNow);

        return Task.FromResult(true);
    }

    public Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || request.SettledAtUtc is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(RoadsideAssistanceRequest.SettledAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySetEscalatedMaintenanceRequestIdAsync(Guid requestId, Guid maintenanceRequestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_requests.TryGetValue(requestId, out var request) || request.EscalatedMaintenanceRequestId is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(request, nameof(RoadsideAssistanceRequest.EscalatedMaintenanceRequestId), maintenanceRequestId);
        SetProperty(request, nameof(RoadsideAssistanceRequest.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyCollection<RoadsideAssistanceRequest>> GetStaleForExpiryAsync(DateTime staleBeforeUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RoadsideAssistanceRequest> stale = _requests.Values
            .Where(r => r.Status is RoadsideRequestStatus.PartnersAvailable or RoadsideRequestStatus.PendingPartnerResponse or RoadsideRequestStatus.Rejected)
            .Where(r => r.RequestedAtUtc < staleBeforeUtc)
            .ToList();

        return Task.FromResult(stale);
    }

    private static void SetProperty(RoadsideAssistanceRequest request, string propertyName, object? value) =>
        typeof(RoadsideAssistanceRequest).GetProperty(propertyName)!.SetValue(request, value);

    private static void SetHistoryProperty(RoadsidePartnerSelectionHistory history, string propertyName, object? value) =>
        typeof(RoadsidePartnerSelectionHistory).GetProperty(propertyName)!.SetValue(history, value);
}
