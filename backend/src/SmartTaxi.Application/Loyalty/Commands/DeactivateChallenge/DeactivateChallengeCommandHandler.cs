using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateChallenge;

public sealed class DeactivateChallengeCommandHandler : ICommandHandler<DeactivateChallengeCommand, Result>
{
    private const string NotFoundError = "Défi introuvable.";

    private readonly ILoyaltyChallengeRepository _repository;

    public DeactivateChallengeCommandHandler(ILoyaltyChallengeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeactivateChallengeCommand command, CancellationToken cancellationToken)
    {
        var challenge = await _repository.GetByIdAsync(command.ChallengeId, cancellationToken);

        if (challenge is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        challenge.Deactivate(DateTime.UtcNow);
        await _repository.UpdateAsync(challenge, cancellationToken);

        return Result.Success();
    }
}
