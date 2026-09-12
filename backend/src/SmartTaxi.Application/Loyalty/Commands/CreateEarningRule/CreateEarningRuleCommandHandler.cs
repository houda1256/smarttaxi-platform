using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Commands.CreateEarningRule;

public sealed class CreateEarningRuleCommandHandler : ICommandHandler<CreateEarningRuleCommand, Result<Guid>>
{
    private const string DuplicateCodeError = "Une règle avec ce code existe déjà.";

    private readonly ILoyaltyEarningRuleRepository _repository;

    public CreateEarningRuleCommandHandler(ILoyaltyEarningRuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateEarningRuleCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.GetByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
        }

        LoyaltyEarningRule rule;

        try
        {
            rule = LoyaltyEarningRule.Create(
                command.Code, command.ActorRole, command.SourceType, command.RewardPointsPerCurrencyUnit, command.StatusPointsPerCurrencyUnit,
                command.MinPoints, command.MaxPoints, command.SubscriptionMultiplierAllowed, command.ValidFrom, command.ValidTo, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(rule, cancellationToken);

        return Result<Guid>.Success(rule.Id);
    }
}
