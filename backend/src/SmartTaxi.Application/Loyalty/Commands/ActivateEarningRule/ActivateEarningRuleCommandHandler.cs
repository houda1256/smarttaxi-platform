using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateEarningRule;

public sealed class ActivateEarningRuleCommandHandler : ICommandHandler<ActivateEarningRuleCommand, Result>
{
    private const string NotFoundError = "Règle de gain introuvable.";

    private readonly ILoyaltyEarningRuleRepository _repository;

    public ActivateEarningRuleCommandHandler(ILoyaltyEarningRuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivateEarningRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);

        if (rule is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        rule.Activate(DateTime.UtcNow);
        await _repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
