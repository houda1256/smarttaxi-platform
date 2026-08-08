using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Queries.GetApplicableTaxRule;

public sealed class GetApplicableTaxRuleQueryHandler : IQueryHandler<GetApplicableTaxRuleQuery, TaxRule?>
{
    private readonly ITaxRuleRepository _taxRuleRepository;

    public GetApplicableTaxRuleQueryHandler(ITaxRuleRepository taxRuleRepository)
    {
        _taxRuleRepository = taxRuleRepository;
    }

    public Task<TaxRule?> Handle(GetApplicableTaxRuleQuery query, CancellationToken cancellationToken) =>
        _taxRuleRepository.GetApplicableRuleAsync(query.ApplicableService, query.Date, cancellationToken);
}
