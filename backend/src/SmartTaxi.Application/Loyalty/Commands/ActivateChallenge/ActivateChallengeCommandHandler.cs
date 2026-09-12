using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateChallenge;

public sealed class ActivateChallengeCommandHandler : ICommandHandler<ActivateChallengeCommand, Result>
{
    private const string NotFoundError = "Défi introuvable.";

    private readonly ILoyaltyChallengeRepository _repository;

    public ActivateChallengeCommandHandler(ILoyaltyChallengeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivateChallengeCommand command, CancellationToken cancellationToken)
    {
        var challenge = await _repository.GetByIdAsync(command.ChallengeId, cancellationToken);

        if (challenge is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        challenge.Activate(DateTime.UtcNow);
        await _repository.UpdateAsync(challenge, cancellationToken);

        return Result.Success();
    }
}
