using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyRedemptionRepository
{
    Task AddAsync(LoyaltyRedemption redemption, CancellationToken cancellationToken);

    Task<LoyaltyRedemption?> GetByIdAsync(Guid redemptionId, CancellationToken cancellationToken);

    Task<PagedResult<LoyaltyRedemption>> GetForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
