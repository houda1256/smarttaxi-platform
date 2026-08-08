using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideStatusHistoryRepository
{
    Task<IReadOnlyCollection<RideStatusHistory>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);
}
