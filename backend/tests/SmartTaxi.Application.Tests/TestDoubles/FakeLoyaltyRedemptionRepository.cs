using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyRedemptionRepository : ILoyaltyRedemptionRepository
{
    private readonly Dictionary<Guid, LoyaltyRedemption> _redemptions = new();

    public Task AddAsync(LoyaltyRedemption redemption, CancellationToken cancellationToken)
    {
        _redemptions[redemption.Id] = redemption;
        return Task.CompletedTask;
    }

    public Task<LoyaltyRedemption?> GetByIdAsync(Guid redemptionId, CancellationToken cancellationToken) =>
        Task.FromResult(_redemptions.GetValueOrDefault(redemptionId));

    public Task<PagedResult<LoyaltyRedemption>> GetForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var ordered = _redemptions.Values.Where(r => r.UserId == userId).OrderByDescending(r => r.RedeemedAtUtc).ToList();
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<LoyaltyRedemption>(page, ordered.Count, pageNumber, pageSize));
    }
}
