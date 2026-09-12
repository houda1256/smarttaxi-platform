using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateEarningRule;

public sealed class UpdateEarningRuleCommandHandler : ICommandHandler<UpdateEarningRuleCommand, Result>
{
    private const string NotFoundError = "Règle de gain introuvable.";

    private readonly ILoyaltyEarningRuleRepository _repository;

    public UpdateEarningRuleCommandHandler(ILoyaltyEarningRuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateEarningRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(command.RuleId, cancellationToken);

        if (rule is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            rule.UpdateDetails(
                command.RewardPointsPerCurrencyUnit, command.StatusPointsPerCurrencyUnit, command.MinPoints, command.MaxPoints,
                command.SubscriptionMultiplierAllowed, command.ValidFrom, command.ValidTo, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.UpdateAsync(rule, cancellationToken);

        return Result.Success();
    }
}
