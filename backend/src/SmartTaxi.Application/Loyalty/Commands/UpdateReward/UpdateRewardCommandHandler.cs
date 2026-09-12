using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateReward;

public sealed class UpdateRewardCommandHandler : ICommandHandler<UpdateRewardCommand, Result>
{
    private const string NotFoundError = "Récompense introuvable.";

    private readonly ILoyaltyRewardRepository _repository;

    public UpdateRewardCommandHandler(ILoyaltyRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateRewardCommand command, CancellationToken cancellationToken)
    {
        var reward = await _repository.GetByIdAsync(command.RewardId, cancellationToken);

        if (reward is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            reward.UpdateDetails(command.Name, command.Description, command.CostInRewardPoints, command.AvailableFrom, command.AvailableTo, command.UsageLimit, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.UpdateAsync(reward, cancellationToken);

        return Result.Success();
    }
}
