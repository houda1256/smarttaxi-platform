using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Commands.CreateReward;

public sealed class CreateRewardCommandHandler : ICommandHandler<CreateRewardCommand, Result<Guid>>
{
    private const string DuplicateCodeError = "Une récompense avec ce code existe déjà.";

    private readonly ILoyaltyRewardRepository _repository;

    public CreateRewardCommandHandler(ILoyaltyRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateRewardCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.GetByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
        }

        LoyaltyReward reward;

        try
        {
            reward = LoyaltyReward.Create(
                command.Code, command.Name, command.Description, command.CostInRewardPoints, command.RewardType, command.TargetRoles,
                command.AvailableFrom, command.AvailableTo, command.UsageLimit, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(reward, cancellationToken);

        return Result<Guid>.Success(reward.Id);
    }
}
