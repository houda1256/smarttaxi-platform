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

    private static string Key(Email email) => email.Value.ToLowerInvariant();
}
