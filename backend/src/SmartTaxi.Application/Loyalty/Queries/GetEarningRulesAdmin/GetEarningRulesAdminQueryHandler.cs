using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetEarningRulesAdmin;

public sealed class GetEarningRulesAdminQueryHandler : IQueryHandler<GetEarningRulesAdminQuery, IReadOnlyCollection<LoyaltyEarningRule>>
{
    private readonly ILoyaltyEarningRuleRepository _repository;

    public GetEarningRulesAdminQueryHandler(ILoyaltyEarningRuleRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<LoyaltyEarningRule>> Handle(GetEarningRulesAdminQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);
}
