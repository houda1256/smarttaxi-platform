using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Commands.CreateChallenge;

public sealed class CreateChallengeCommandHandler : ICommandHandler<CreateChallengeCommand, Result<Guid>>
{
    private const string DuplicateCodeError = "Un défi avec ce code existe déjà.";

    private readonly ILoyaltyChallengeRepository _repository;

    public CreateChallengeCommandHandler(ILoyaltyChallengeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateChallengeCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.GetByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
        }

        LoyaltyChallenge challenge;

        try
        {
            challenge = LoyaltyChallenge.Create(
                command.Code, command.Name, command.CriteriaType, command.TargetValue, command.RewardPoints, command.EligibleRole,
                command.ValidFrom, command.ValidTo, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(challenge, cancellationToken);

        return Result<Guid>.Success(challenge.Id);
    }
}
