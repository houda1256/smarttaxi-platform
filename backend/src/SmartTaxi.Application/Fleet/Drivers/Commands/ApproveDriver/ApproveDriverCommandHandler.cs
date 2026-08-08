using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.ApproveDriver;

/// <summary>
/// Reuses Identity's DocumentEligibilityChecker (2d/2e) for the Driver role's
/// critical documents (driver's license, criminal record) — a driver cannot
/// become operational without the required Identity-level documents approved,
/// same hard gate as professional-account approval.
/// </summary>
public sealed class ApproveDriverCommandHandler : ICommandHandler<ApproveDriverCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";
    private const string NotUnderReviewError = "Ce profil n'est pas en cours de revue.";
    private const string SelfApprovalError = "Un chauffeur ne peut pas approuver son propre profil.";
    private const string NotEligibleError = "Les documents requis pour ce chauffeur ne sont pas tous approuvés.";

    private readonly IDriverProfileRepository _repository;
    private readonly DocumentEligibilityChecker _eligibilityChecker;

    public ApproveDriverCommandHandler(IDriverProfileRepository repository, DocumentEligibilityChecker eligibilityChecker)
    {
        _repository = repository;
        _eligibilityChecker = eligibilityChecker;
    }

    public async Task<Result> Handle(ApproveDriverCommand command, CancellationToken cancellationToken)
    {
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

        var utcNow = DateTime.UtcNow;
        var eligibility = await _eligibilityChecker.CheckAsync(profile.UserId, UserRole.Driver, utcNow, cancellationToken);

        if (!eligibility.IsEligible)
        {
            return Result.Failure(NotEligibleError, ErrorType.Validation);
        }

        var approved = await _repository.TryApproveAsync(profile.Id, utcNow, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotUnderReviewError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
