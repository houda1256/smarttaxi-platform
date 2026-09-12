using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<LoyaltyAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>False when the unique UserId index rejects a concurrent duplicate account open — same "atomic guard at the DB level" convention as SubscriptionRepository.TryAddAsync.</summary>
    Task<bool> TryAddAsync(LoyaltyAccount account, CancellationToken cancellationToken);
}
