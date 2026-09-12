using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Application.Identity.Referrals.Commands.InvalidateReferral;

/// <summary>Admin fraud action — e.g., a detected self-referral loop or fake-account ring.</summary>
public sealed class InvalidateReferralCommandHandler : ICommandHandler<InvalidateReferralCommand, Result>
{
    private const string NotFoundError = "Parrainage introuvable.";
    private const string AlreadyInvalidatedError = "Ce parrainage est déjà invalidé.";

    private readonly IReferralRepository _repository;

    public InvalidateReferralCommandHandler(IReferralRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(InvalidateReferralCommand command, CancellationToken cancellationToken)
    {
        var referral = await _repository.GetByIdAsync(command.ReferralId, cancellationToken);

        if (referral is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (referral.Status == ReferralStatus.Invalidated)
        {
            return Result.Failure(AlreadyInvalidatedError, ErrorType.Conflict);
        }

        var invalidated = await _repository.TryInvalidateAsync(referral.Id, DateTime.UtcNow, cancellationToken);

        if (!invalidated)
        {
            return Result.Failure(AlreadyInvalidatedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
