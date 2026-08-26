using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetRewardCatalogAdmin;

public sealed class GetRewardCatalogAdminQueryHandler : IQueryHandler<GetRewardCatalogAdminQuery, IReadOnlyCollection<LoyaltyReward>>
{
    private readonly ILoyaltyRewardRepository _repository;

    public GetRewardCatalogAdminQueryHandler(ILoyaltyRewardRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<LoyaltyReward>> Handle(GetRewardCatalogAdminQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);
}
