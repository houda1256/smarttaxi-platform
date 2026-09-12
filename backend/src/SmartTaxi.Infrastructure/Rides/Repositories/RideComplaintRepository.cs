using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideComplaintRepository : IRideComplaintRepository
{
    private readonly ApplicationDbContext _context;

    public RideComplaintRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideComplaint complaint, CancellationToken cancellationToken)
    {
        await _context.RideComplaints.AddAsync(complaint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RideComplaint?> GetByIdAsync(Guid complaintId, CancellationToken cancellationToken) =>
        _context.RideComplaints.FirstOrDefaultAsync(complaint => complaint.Id == complaintId, cancellationToken);

    public async Task<IReadOnlyCollection<RideComplaint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideComplaints.Where(complaint => complaint.RideId == rideId).ToListAsync(cancellationToken);

    public Task<bool> TryResolveAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(complaintId, RideComplaintStatus.Resolved, resolution, utcNow, cancellationToken);

    public Task<bool> TryDismissAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(complaintId, RideComplaintStatus.Dismissed, resolution, utcNow, cancellationToken);

    private async Task<bool> TryTransitionAsync(
        Guid complaintId, RideComplaintStatus to, string resolution, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RideComplaints
            .Where(complaint => complaint.Id == complaintId
                && complaint.Status != RideComplaintStatus.Resolved && complaint.Status != RideComplaintStatus.Dismissed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(complaint => complaint.Status, to)
                .SetProperty(complaint => complaint.Resolution, resolution)
                .SetProperty(complaint => complaint.ResolvedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
