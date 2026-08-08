using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Commands.CreateTaxRule;

public sealed class CreateTaxRuleCommandHandler : ICommandHandler<CreateTaxRuleCommand, Result<Guid>>
{
    private readonly ITaxRuleRepository _taxRuleRepository;

    public CreateTaxRuleCommandHandler(ITaxRuleRepository taxRuleRepository)
    {
        _taxRuleRepository = taxRuleRepository;
    }

    public async Task<Result<Guid>> Handle(CreateTaxRuleCommand command, CancellationToken cancellationToken)
    {
        TaxRule rule;

        try
        {
            rule = TaxRule.Create(
                command.TaxName, command.TaxRate, command.Jurisdiction, command.ApplicableService,
                command.EffectiveFrom, command.EffectiveTo, command.ExemptionRules, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _taxRuleRepository.AddAsync(rule, cancellationToken);

        return Result<Guid>.Success(rule.Id);
    }
}
