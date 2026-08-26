using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly Dictionary<Guid, LoyaltyAccount> _accounts = new();

    public Task<LoyaltyAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.Values.FirstOrDefault(a => a.UserId == userId));

    public Task<LoyaltyAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.GetValueOrDefault(accountId));

    public Task<bool> TryAddAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        if (_accounts.Values.Any(a => a.UserId == account.UserId))
        {
            return Task.FromResult(false);
        }

        _accounts[account.Id] = account;
        return Task.FromResult(true);
    }
}
