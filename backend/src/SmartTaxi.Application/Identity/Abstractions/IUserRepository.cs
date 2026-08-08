using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}
