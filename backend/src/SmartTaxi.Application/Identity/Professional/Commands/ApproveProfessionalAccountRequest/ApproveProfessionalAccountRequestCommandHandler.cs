using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Identity.Professional.Commands.ApproveProfessionalAccountRequest;

/// <summary>
/// A professional account must never become active until all required
/// documents are approved — this is enforced here, not just hinted at in a
/// UI, so approval is impossible to bypass through the API.
/// </summary>
public sealed class ApproveProfessionalAccountRequestCommandHandler
    : ICommandHandler<ApproveProfessionalAccountRequestCommand, Result>
{
    private const string NotFoundError = "Demande introuvable.";
    private const string NotPendingError = "Cette demande n'est plus en attente de revue.";
    private const string NotEligibleError = "Les documents requis pour ce rôle ne sont pas tous approuvés.";

    private readonly IProfessionalAccountRequestRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly DocumentEligibilityChecker _eligibilityChecker;

    public ApproveProfessionalAccountRequestCommandHandler(
        IProfessionalAccountRequestRepository repository, IUserRepository userRepository,
        DocumentEligibilityChecker eligibilityChecker)
    {
        _repository = repository;
        _userRepository = userRepository;
        _eligibilityChecker = eligibilityChecker;
    }

    public async Task<Result> Handle(ApproveProfessionalAccountRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != ProfessionalAccountStatus.PendingReview)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var eligibility = await _eligibilityChecker.CheckAsync(request.UserId, request.Role, utcNow, cancellationToken);

        if (!eligibility.IsEligible)
        {
            return Result.Failure(NotEligibleError, ErrorType.Validation);
        }

        var approved = await _repository.TryApproveAsync(
            request.Id, command.ReviewerId, utcNow, command.ReviewComment, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is not null)
        {
            user.AssignRole(request.Role);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return Result.Success();
    }
}
