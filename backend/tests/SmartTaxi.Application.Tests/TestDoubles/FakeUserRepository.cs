using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _usersByEmail = new();
    private readonly Dictionary<Guid, User> _usersById = new();

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
        => Task.FromResult(_usersByEmail.ContainsKey(Key(email)));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
        => Task.FromResult(_usersByEmail.GetValueOrDefault(Key(email)));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_usersById.GetValueOrDefault(id));

    public Task<User?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken)
        => Task.FromResult(_usersById.Values.FirstOrDefault(u => u.ReferralCode == referralCode));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _usersByEmail[Key(user.Email)] = user;
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        _usersByEmail[Key(user.Email)] = user;
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<int> RecordFailedLoginAttemptAsync(
        Guid userId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        var user = _usersById[userId];
        var newCount = user.FailedLoginAttempts + 1;
        SetProperty(user, nameof(User.FailedLoginAttempts), newCount);

        if (newCount >= maxAttempts)
        {
            SetProperty(user, nameof(User.LockedUntilUtc), (DateTime?)utcNow.Add(lockoutDuration));
        }

        return Task.FromResult(newCount);
    }

    public Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (_usersById.TryGetValue(userId, out var user))
        {
            SetProperty(user, nameof(User.FailedLoginAttempts), 0);
            SetProperty(user, nameof(User.LockedUntilUtc), (DateTime?)null);
        }

        return Task.CompletedTask;
    }

    private static string Key(Email email) => email.Value.ToLowerInvariant();

    // User has no public mutators for these fields by design — real mutation
    // happens via atomic repository-level SQL in production; this fake uses
    // reflection to simulate that same atomicity in memory for tests.
    private static void SetProperty(User user, string propertyName, object? value) =>
        typeof(User).GetProperty(propertyName)!.SetValue(user, value);
}
