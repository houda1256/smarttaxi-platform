using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideShareTokenRepository : IRideShareTokenRepository
{
    private readonly Dictionary<Guid, RideShareToken> _tokensById = new();

    public Task AddAsync(RideShareToken token, CancellationToken cancellationToken)
    {
        _tokensById[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task<RideShareToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken) =>
        Task.FromResult(_tokensById.GetValueOrDefault(tokenId));

    public Task<RideShareToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = _tokensById.Values.FirstOrDefault(t => t.TokenHash == tokenHash);
        return Task.FromResult(token);
    }

    public Task<RideShareToken?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var token = _tokensById.Values.FirstOrDefault(t => t.RideId == rideId && t.IsValid(DateTime.UtcNow));
        return Task.FromResult(token);
    }

    public Task<bool> TryRevokeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tokensById.TryGetValue(tokenId, out var token) || token.RevokedAt is not null)
        {
            return Task.FromResult(false);
        }

        typeof(RideShareToken).GetProperty(nameof(RideShareToken.RevokedAt))!.SetValue(token, utcNow);
        return Task.FromResult(true);
    }
}
