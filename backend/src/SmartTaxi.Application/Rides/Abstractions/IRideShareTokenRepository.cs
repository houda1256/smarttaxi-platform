using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideShareTokenRepository
{
    Task AddAsync(RideShareToken token, CancellationToken cancellationToken);

    Task<RideShareToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken);

    Task<RideShareToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<RideShareToken?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<bool> TryRevokeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken);
}
