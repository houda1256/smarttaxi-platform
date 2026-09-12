using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SubmitDriverForReview;

public sealed class SubmitDriverForReviewCommandHandler : ICommandHandler<SubmitDriverForReviewCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";
    private const string NotPendingError = "Ce profil n'est pas en attente de soumission.";

    private readonly IDriverProfileRepository _repository;

    public SubmitDriverForReviewCommandHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitDriverForReviewCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.DriverProfileId, cancellationToken);

        if (profile is null || profile.UserId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (profile.VerificationStatus != DriverVerificationStatus.PendingReview)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var submitted = await _repository.TrySubmitForReviewAsync(profile.Id, DateTime.UtcNow, cancellationToken);

        if (!submitted)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
