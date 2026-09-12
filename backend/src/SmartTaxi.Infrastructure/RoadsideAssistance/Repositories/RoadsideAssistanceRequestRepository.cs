using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

internal sealed class RoadsideAssistanceRequestRepository : IRoadsideAssistanceRequestRepository
{
    /// <summary>Terminal statuses — excluded from the partial unique index and from "active" queries. Rejected is deliberately NOT terminal (approved plan, Q1): a rejected cycle still occupies the vehicle's one-active-request slot pending an explicit requester reselect/cancel/expire.</summary>
    internal static readonly RoadsideRequestStatus[] TerminalStatuses =
    [
        RoadsideRequestStatus.Completed, RoadsideRequestStatus.Cancelled, RoadsideRequestStatus.Expired, RoadsideRequestStatus.Disputed
    ];

    private readonly ApplicationDbContext _context;

    public RoadsideAssistanceRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(RoadsideAssistanceRequest request, CancellationToken cancellationToken)
    {
        await _context.RoadsideAssistanceRequests.AddAsync(request, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(request).State = EntityState.Detached;
            return false;
        }
    }

    public Task<RoadsideAssistanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        _context.RoadsideAssistanceRequests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<PagedResult<RoadsideAssistanceRequest>> GetForRequesterAsync(
        Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.RoadsideAssistanceRequests.Where(request => request.RequesterUserId == requesterUserId);
        return await ToPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<RoadsideAssistanceRequest>> GetForPartnerAsync(
        Guid partnerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var requestIds = _context.RoadsidePartnerSelectionHistories
            .Where(history => history.SelectedPartnerUserId == partnerUserId)
            .Select(history => history.RoadsideAssistanceRequestId);

        var query = _context.RoadsideAssistanceRequests.Where(request => requestIds.Contains(request.Id));
        return await ToPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<RoadsideAssistanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await ToPagedResultAsync(_context.RoadsideAssistanceRequests, pageNumber, pageSize, cancellationToken);

    public Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        _context.RoadsideAssistanceRequests.AnyAsync(
            request => request.VehicleId == vehicleId && !TerminalStatuses.Contains(request.Status), cancellationToken);

    public async Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<RoadsideRequestStatus> allowedFromStatuses, RoadsideRequestStatus newStatus,
        Guid? requiredRequesterUserId, Guid? requiredPartnerUserId, decimal? finalCost, string? reason, Guid? cancelledByUserId,
        bool clearSelectedPartner, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RoadsideAssistanceRequests
            .Where(request => request.Id == requestId && allowedFromStatuses.Contains(request.Status)
                && (requiredRequesterUserId == null || request.RequesterUserId == requiredRequesterUserId)
                && (requiredPartnerUserId == null || request.SelectedPartnerUserId == requiredPartnerUserId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, newStatus)
                .SetProperty(request => request.UpdatedAtUtc, utcNow)
                .SetProperty(
                    request => request.SelectedPartnerUserId,
                    request => clearSelectedPartner ? null : request.SelectedPartnerUserId)
                .SetProperty(
                    request => request.PartnerOnTheWayAtUtc,
                    request => newStatus == RoadsideRequestStatus.PartnerOnTheWay ? utcNow : request.PartnerOnTheWayAtUtc)
                .SetProperty(
                    request => request.PartnerArrivedAtUtc,
                    request => newStatus == RoadsideRequestStatus.PartnerArrived ? utcNow : request.PartnerArrivedAtUtc)
                .SetProperty(
                    request => request.StartedAtUtc, request => newStatus == RoadsideRequestStatus.InProgress ? utcNow : request.StartedAtUtc)
                .SetProperty(
                    request => request.FinalCost, request => newStatus == RoadsideRequestStatus.Completed ? finalCost : request.FinalCost)
                .SetProperty(
                    request => request.CompletedAtUtc,
                    request => newStatus == RoadsideRequestStatus.Completed ? utcNow : request.CompletedAtUtc)
                .SetProperty(
                    request => request.CancelledAtUtc, request => newStatus == RoadsideRequestStatus.Cancelled ? utcNow : request.CancelledAtUtc)
                .SetProperty(
                    request => request.CancellationReason,
                    request => newStatus == RoadsideRequestStatus.Cancelled ? reason : request.CancellationReason)
                .SetProperty(
                    request => request.CancelledByUserId,
                    request => newStatus == RoadsideRequestStatus.Cancelled ? cancelledByUserId : request.CancelledByUserId)
                .SetProperty(
                    request => request.ExpiredAtUtc, request => newStatus == RoadsideRequestStatus.Expired ? utcNow : request.ExpiredAtUtc)
                .SetProperty(
                    request => request.DisputedAtUtc, request => newStatus == RoadsideRequestStatus.Disputed ? utcNow : request.DisputedAtUtc),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySelectPartnerAsync(
        Guid requestId, Guid requesterUserId, Guid partnerUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.RoadsideAssistanceRequests
            .Where(request => request.Id == requestId && request.Status == RoadsideRequestStatus.PartnersAvailable
                && request.RequesterUserId == requesterUserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, RoadsideRequestStatus.PendingPartnerResponse)
                .SetProperty(request => request.SelectedPartnerUserId, partnerUserId)
                .SetProperty(request => request.UpdatedAtUtc, utcNow), cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var cycleNumber = await _context.RoadsidePartnerSelectionHistories
            .CountAsync(history => history.RoadsideAssistanceRequestId == requestId, cancellationToken) + 1;

        await _context.RoadsidePartnerSelectionHistories.AddAsync(
            new RoadsidePartnerSelectionHistory(requestId, cycleNumber, partnerUserId, utcNow), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRespondAsync(
        Guid requestId, Guid partnerUserId, RoadsidePartnerResponse response, decimal? estimatedCost, string? rejectionReason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var newStatus = response == RoadsidePartnerResponse.Accepted ? RoadsideRequestStatus.Accepted : RoadsideRequestStatus.Rejected;

        var rows = await _context.RoadsideAssistanceRequests
            .Where(request => request.Id == requestId && request.Status == RoadsideRequestStatus.PendingPartnerResponse
                && request.SelectedPartnerUserId == partnerUserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, newStatus)
                .SetProperty(request => request.UpdatedAtUtc, utcNow)
                .SetProperty(
                    request => request.AcceptedAtUtc,
                    request => response == RoadsidePartnerResponse.Accepted ? utcNow : request.AcceptedAtUtc)
                .SetProperty(
                    request => request.EstimatedCost,
                    request => response == RoadsidePartnerResponse.Accepted ? estimatedCost : request.EstimatedCost),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var historyRows = await _context.RoadsidePartnerSelectionHistories
            .Where(history => history.RoadsideAssistanceRequestId == requestId && history.Response == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(history => history.Response, response)
                .SetProperty(history => history.RejectionReason, rejectionReason)
                .SetProperty(history => history.RespondedAtUtc, utcNow), cancellationToken);

        if (historyRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RoadsideAssistanceRequests
            .Where(request => request.Id == requestId && request.SettledAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.SettledAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySetEscalatedMaintenanceRequestIdAsync(
        Guid requestId, Guid maintenanceRequestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RoadsideAssistanceRequests
            .Where(request => request.Id == requestId && request.EscalatedMaintenanceRequestId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.EscalatedMaintenanceRequestId, maintenanceRequestId)
                .SetProperty(request => request.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<IReadOnlyCollection<RoadsideAssistanceRequest>> GetStaleForExpiryAsync(DateTime staleBeforeUtc, CancellationToken cancellationToken) =>
        await _context.RoadsideAssistanceRequests
            .Where(request => request.Status == RoadsideRequestStatus.PartnersAvailable
                || request.Status == RoadsideRequestStatus.PendingPartnerResponse || request.Status == RoadsideRequestStatus.Rejected)
            .Where(request => request.RequestedAtUtc < staleBeforeUtc)
            .ToListAsync(cancellationToken);

    private static async Task<PagedResult<RoadsideAssistanceRequest>> ToPagedResultAsync(
        IQueryable<RoadsideAssistanceRequest> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(request => request.RequestedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoadsideAssistanceRequest>(items, totalCount, pageNumber, pageSize);
    }
}
