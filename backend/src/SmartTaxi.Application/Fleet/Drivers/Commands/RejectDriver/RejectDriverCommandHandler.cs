using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.RejectDriver;

/// <summary>
/// The Reason is required for the response/notification but is not persisted
/// — DriverProfile's field list (per spec) has no RejectionReason column,
/// unlike ProfessionalAccountRequest/UserDocument.
/// </summary>
public sealed class RejectDriverCommandHandler : ICommandHandler<RejectDriverCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";
    private const string NotUnderReviewError = "Ce profil n'est pas en cours de revue.";
    private const string SelfApprovalError = "Un chauffeur ne peut pas examiner son propre profil.";
    private const string MissingReasonError = "Un motif de rejet est requis.";

    private readonly IDriverProfileRepository _repository;

    public RejectDriverCommandHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectDriverCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(MissingReasonError, ErrorType.Validation);
        }

        var profile = await _repository.GetByIdAsync(command.DriverProfileId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (profile.UserId == command.ReviewerId)
        {
            return Result.Failure(SelfApprovalError, ErrorType.Forbidden);
        }

        if (profile.VerificationStatus != DriverVerificationStatus.UnderReview)
        {
            return Result.Failure(NotUnderReviewError, ErrorType.Conflict);
        }

        var rejected = await _repository.TryRejectAsync(profile.Id, DateTime.UtcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotUnderReviewError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
