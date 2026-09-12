using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetRewardCatalog;

public sealed class GetRewardCatalogQueryHandler : IQueryHandler<GetRewardCatalogQuery, IReadOnlyCollection<LoyaltyReward>>
{
    private readonly ILoyaltyRewardRepository _rewardRepository;

    public GetRewardCatalogQueryHandler(ILoyaltyRewardRepository rewardRepository)
    {
        _rewardRepository = rewardRepository;
    }

    public Task<IReadOnlyCollection<LoyaltyReward>> Handle(GetRewardCatalogQuery query, CancellationToken cancellationToken) =>
        _rewardRepository.GetAvailableAsync(query.Role, DateTime.UtcNow, cancellationToken);
}
