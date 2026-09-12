using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideComplaintRepository : IRideComplaintRepository
{
    private readonly Dictionary<Guid, RideComplaint> _complaintsById = new();

    public Task AddAsync(RideComplaint complaint, CancellationToken cancellationToken)
    {
        _complaintsById[complaint.Id] = complaint;
        return Task.CompletedTask;
    }

    public Task<RideComplaint?> GetByIdAsync(Guid complaintId, CancellationToken cancellationToken) =>
        Task.FromResult(_complaintsById.GetValueOrDefault(complaintId));

    public Task<IReadOnlyCollection<RideComplaint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideComplaint> complaints = _complaintsById.Values.Where(c => c.RideId == rideId).ToList();
        return Task.FromResult(complaints);
    }

    public Task<bool> TryResolveAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(complaintId, resolution, utcNow, RideComplaintStatus.Resolved);

    public Task<bool> TryDismissAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(complaintId, resolution, utcNow, RideComplaintStatus.Dismissed);

    private Task<bool> TryTransition(Guid complaintId, string resolution, DateTime utcNow, RideComplaintStatus to)
    {
        if (!_complaintsById.TryGetValue(complaintId, out var complaint)
            || complaint.Status is RideComplaintStatus.Resolved or RideComplaintStatus.Dismissed)
        {
            return Task.FromResult(false);
        }

        typeof(RideComplaint).GetProperty(nameof(RideComplaint.Status))!.SetValue(complaint, to);
        typeof(RideComplaint).GetProperty(nameof(RideComplaint.Resolution))!.SetValue(complaint, resolution);
        typeof(RideComplaint).GetProperty(nameof(RideComplaint.ResolvedAt))!.SetValue(complaint, utcNow);
        return Task.FromResult(true);
    }
}
