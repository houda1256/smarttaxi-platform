using SmartTaxi.Application.Administration.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Mirrors the real repository's WHERE-guarded semantics in memory: each Try* only succeeds from the correct prior state.</summary>
public sealed class FakeAdminUserManagementRepository : IAdminUserManagementRepository
{
    private readonly Dictionary<Guid, bool> _isActiveByUserId = new();
    private readonly Dictionary<Guid, bool> _twoFactorEnabledByUserId = new();
    private readonly Dictionary<Guid, int> _activeSessionCountByUserId = new();

    public void SeedUser(Guid userId, bool isActive = true, bool twoFactorEnabled = false, int activeSessionCount = 0)
    {
        _isActiveByUserId[userId] = isActive;
        _twoFactorEnabledByUserId[userId] = twoFactorEnabled;
        _activeSessionCountByUserId[userId] = activeSessionCount;
    }

    public Task<bool> TrySuspendAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_isActiveByUserId.TryGetValue(targetUserId, out var isActive) || !isActive)
        {
            return Task.FromResult(false);
        }

        _isActiveByUserId[targetUserId] = false;
        _activeSessionCountByUserId[targetUserId] = 0;
        return Task.FromResult(true);
    }

    public Task<bool> TryReactivateAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_isActiveByUserId.TryGetValue(targetUserId, out var isActive) || isActive)
        {
            return Task.FromResult(false);
        }

        _isActiveByUserId[targetUserId] = true;
        return Task.FromResult(true);
    }

    public Task<int?> TryRevokeSessionsAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_isActiveByUserId.ContainsKey(targetUserId))
        {
            return Task.FromResult((int?)null);
        }

        var count = _activeSessionCountByUserId.GetValueOrDefault(targetUserId);
        _activeSessionCountByUserId[targetUserId] = 0;
        return Task.FromResult((int?)count);
    }

    public Task<bool> TryResetTwoFactorAsync(Guid targetUserId, Guid actorUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_twoFactorEnabledByUserId.TryGetValue(targetUserId, out var enabled) || !enabled)
        {
            return Task.FromResult(false);
        }

        _twoFactorEnabledByUserId[targetUserId] = false;
        return Task.FromResult(true);
    }
}
