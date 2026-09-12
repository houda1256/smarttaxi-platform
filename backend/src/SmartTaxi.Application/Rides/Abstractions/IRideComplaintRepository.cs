using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideComplaintRepository
{
    Task AddAsync(RideComplaint complaint, CancellationToken cancellationToken);

    Task<RideComplaint?> GetByIdAsync(Guid complaintId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideComplaint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<bool> TryResolveAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDismissAsync(Guid complaintId, string resolution, DateTime utcNow, CancellationToken cancellationToken);
}
