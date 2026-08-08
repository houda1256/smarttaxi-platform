using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Taxes.Abstractions;

namespace SmartTaxi.Application.Payments.Taxes.Commands.DeactivateTaxRule;

public sealed class DeactivateTaxRuleCommandHandler : ICommandHandler<DeactivateTaxRuleCommand, Result>
{
    private const string NotFoundError = "Règle de taxe introuvable.";
    private const string AlreadyInactiveError = "Cette règle de taxe est déjà inactive.";

    private readonly ITaxRuleRepository _taxRuleRepository;

    public DeactivateTaxRuleCommandHandler(ITaxRuleRepository taxRuleRepository)
    {
        _taxRuleRepository = taxRuleRepository;
    }

    public async Task<Result> Handle(DeactivateTaxRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _taxRuleRepository.GetByIdAsync(command.TaxRuleId, cancellationToken);

        if (rule is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var deactivated = await _taxRuleRepository.TryDeactivateAsync(command.TaxRuleId, DateTime.UtcNow, cancellationToken);

        return deactivated ? Result.Success() : Result.Failure(AlreadyInactiveError, ErrorType.Conflict);
    }
}
