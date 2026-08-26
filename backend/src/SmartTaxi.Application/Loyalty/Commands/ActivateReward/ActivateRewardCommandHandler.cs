using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateReward;

public sealed class ActivateRewardCommandHandler : ICommandHandler<ActivateRewardCommand, Result>
{
    private const string NotFoundError = "Récompense introuvable.";

    private readonly ILoyaltyRewardRepository _repository;

    public ActivateRewardCommandHandler(ILoyaltyRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivateRewardCommand command, CancellationToken cancellationToken)
    {
        var reward = await _repository.GetByIdAsync(command.RewardId, cancellationToken);

        if (reward is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        reward.Activate(DateTime.UtcNow);
        await _repository.UpdateAsync(reward, cancellationToken);

        return Result.Success();
    }
}
