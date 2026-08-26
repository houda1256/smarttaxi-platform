using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDeviceTokenRepository : IDeviceTokenRepository
{
    private readonly Dictionary<Guid, DeviceToken> _tokens = new();

    public Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.Values.FirstOrDefault(t => t.Token == token));

    public Task<DeviceToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.GetValueOrDefault(id));

    public Task<bool> TryAddAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        if (_tokens.Values.Any(t => t.Token == token.Token && t.IsActive))
        {
            return Task.FromResult(false);
        }

        _tokens[token.Id] = token;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        _tokens[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DeviceToken>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<DeviceToken>>(_tokens.Values.Where(t => t.UserId == userId && t.IsActive).ToList());
}
