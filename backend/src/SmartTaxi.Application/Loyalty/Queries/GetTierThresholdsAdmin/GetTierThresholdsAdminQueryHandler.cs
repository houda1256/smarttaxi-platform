using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetTierThresholdsAdmin;

public sealed class GetTierThresholdsAdminQueryHandler : IQueryHandler<GetTierThresholdsAdminQuery, IReadOnlyCollection<LoyaltyTierThreshold>>
{
    private readonly ILoyaltyTierThresholdRepository _repository;

    public GetTierThresholdsAdminQueryHandler(ILoyaltyTierThresholdRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<LoyaltyTierThreshold>> Handle(GetTierThresholdsAdminQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);
}
