using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateEarningRule;

public sealed class DeactivateEarningRuleCommandHandler : ICommandHandler<DeactivateEarningRuleCommand, Result>
{
    private const string NotFoundError = "Règle de gain introuvable.";

    private readonly ILoyaltyEarningRuleRepository _repository;

    public DeactivateEarningRuleCommandHandler(ILoyaltyEarningRuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeactivateEarningRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);

        if (rule is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        rule.Deactivate(DateTime.UtcNow);
        await _repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
