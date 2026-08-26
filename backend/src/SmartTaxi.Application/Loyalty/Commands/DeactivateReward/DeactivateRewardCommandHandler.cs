using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateReward;

public sealed class DeactivateRewardCommandHandler : ICommandHandler<DeactivateRewardCommand, Result>
{
    private const string NotFoundError = "Récompense introuvable.";

    private readonly ILoyaltyRewardRepository _repository;

    public DeactivateRewardCommandHandler(ILoyaltyRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeactivateRewardCommand command, CancellationToken cancellationToken)
    {
        var reward = await _repository.GetByIdAsync(command.RewardId, cancellationToken);

        if (reward is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        reward.Deactivate(DateTime.UtcNow);
        await _repository.UpdateAsync(reward, cancellationToken);

        return Result.Success();
    }
}
