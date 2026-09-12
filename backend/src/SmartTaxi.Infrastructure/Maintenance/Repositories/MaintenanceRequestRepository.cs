using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

internal sealed class MaintenanceRequestRepository : IMaintenanceRequestRepository
{
    /// <summary>Terminal statuses — excluded from the partial unique index and from "active" queries. Disputed is deliberately NOT terminal: an unresolved dispute still occupies the vehicle's one-active-request slot and remains eligible for admin ForceCancel.</summary>
    internal static readonly MaintenanceRequestStatus[] TerminalStatuses =
    [
        MaintenanceRequestStatus.Completed, MaintenanceRequestStatus.Cancelled, MaintenanceRequestStatus.Rejected,
        MaintenanceRequestStatus.QuoteRejected
    ];

    private readonly ApplicationDbContext _context;

    public MaintenanceRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(MaintenanceRequest request, CancellationToken cancellationToken)
    {
        await _context.MaintenanceRequests.AddAsync(request, cancellationToken);

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

    public Task<MaintenanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        _context.MaintenanceRequests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<PagedResult<MaintenanceRequest>> GetForOwnerAsync(Guid ownerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.MaintenanceRequests.Where(request => request.OwnerUserId == ownerUserId);
        return await ToPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<MaintenanceRequest>> GetForGarageAsync(Guid garageUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.MaintenanceRequests.Where(request => request.GarageUserId == garageUserId);
        return await ToPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedResult<MaintenanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        await ToPagedResultAsync(_context.MaintenanceRequests, pageNumber, pageSize, cancellationToken);

    public async Task<IReadOnlyCollection<MaintenanceRequest>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await _context.MaintenanceRequests.Where(request => request.VehicleId == vehicleId)
            .OrderByDescending(request => request.RequestedAtUtc).ToListAsync(cancellationToken);

    public Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        _context.MaintenanceRequests.AnyAsync(
            request => request.VehicleId == vehicleId && !TerminalStatuses.Contains(request.Status), cancellationToken);

    public async Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<MaintenanceRequestStatus> allowedFromStatuses, MaintenanceRequestStatus newStatus,
        Guid? requiredGarageUserId, Guid? requiredOwnerUserId, decimal? estimatedCost, decimal? finalCost, string? reason,
        Guid? cancelledByUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.MaintenanceRequests
            .Where(request => request.Id == requestId && allowedFromStatuses.Contains(request.Status)
                && (requiredGarageUserId == null || request.GarageUserId == requiredGarageUserId)
                && (requiredOwnerUserId == null || request.OwnerUserId == requiredOwnerUserId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, newStatus)
                .SetProperty(request => request.UpdatedAtUtc, utcNow)
                .SetProperty(
                    request => request.ConfirmedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.QuotePending ? utcNow : request.ConfirmedAtUtc)
                .SetProperty(
                    request => request.GarageRejectionReason,
                    request => newStatus == MaintenanceRequestStatus.Rejected ? reason : request.GarageRejectionReason)
                .SetProperty(
                    request => request.EstimatedCost,
                    request => newStatus == MaintenanceRequestStatus.QuoteSubmitted ? estimatedCost : request.EstimatedCost)
                .SetProperty(
                    request => request.QuoteSubmittedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.QuoteSubmitted ? utcNow : request.QuoteSubmittedAtUtc)
                .SetProperty(
                    request => request.QuoteAcceptedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.QuoteAccepted ? utcNow : request.QuoteAcceptedAtUtc)
                .SetProperty(
                    request => request.VehicleReceivedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.VehicleReceived ? utcNow : request.VehicleReceivedAtUtc)
                .SetProperty(
                    request => request.WorkStartedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.InProgress && request.Status == MaintenanceRequestStatus.VehicleReceived
                        ? utcNow
                        : request.WorkStartedAtUtc)
                .SetProperty(
                    request => request.FinalCost, request => newStatus == MaintenanceRequestStatus.Completed ? finalCost : request.FinalCost)
                .SetProperty(
                    request => request.CompletedAtUtc,
                    request => newStatus == MaintenanceRequestStatus.Completed ? utcNow : request.CompletedAtUtc)
                .SetProperty(
                    request => request.CancelledAtUtc,
                    request => newStatus == MaintenanceRequestStatus.Cancelled ? utcNow : request.CancelledAtUtc)
                .SetProperty(
                    request => request.CancellationReason,
                    request => newStatus == MaintenanceRequestStatus.Cancelled ? reason : request.CancellationReason)
                .SetProperty(
                    request => request.CancelledByUserId,
                    request => newStatus == MaintenanceRequestStatus.Cancelled ? cancelledByUserId : request.CancelledByUserId),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.MaintenanceRequests
            .Where(request => request.Id == requestId && request.SettledAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.SettledAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    private static async Task<PagedResult<MaintenanceRequest>> ToPagedResultAsync(
        IQueryable<MaintenanceRequest> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(request => request.RequestedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MaintenanceRequest>(items, totalCount, pageNumber, pageSize);
    }
}
