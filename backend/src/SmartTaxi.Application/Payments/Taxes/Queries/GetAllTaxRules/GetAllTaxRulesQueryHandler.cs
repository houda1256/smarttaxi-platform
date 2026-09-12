using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Queries.GetAllTaxRules;

public sealed class GetAllTaxRulesQueryHandler : IQueryHandler<GetAllTaxRulesQuery, PagedResult<TaxRule>>
{
    private readonly ITaxRuleRepository _taxRuleRepository;

    public GetAllTaxRulesQueryHandler(ITaxRuleRepository taxRuleRepository)
    {
        _taxRuleRepository = taxRuleRepository;
    }

    public Task<PagedResult<TaxRule>> Handle(GetAllTaxRulesQuery query, CancellationToken cancellationToken) =>
        _taxRuleRepository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
