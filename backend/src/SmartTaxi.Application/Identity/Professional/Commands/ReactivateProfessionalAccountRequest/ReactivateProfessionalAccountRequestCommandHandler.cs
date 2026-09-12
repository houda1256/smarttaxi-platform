using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Identity.Professional.Commands.ReactivateProfessionalAccountRequest;

/// <summary>
/// Re-checks document eligibility on reactivation too — documents may have
/// expired during the suspension window, and the "never active without
/// approved documents" invariant applies here just as much as on first approval.
/// </summary>
public sealed class ReactivateProfessionalAccountRequestCommandHandler
    : ICommandHandler<ReactivateProfessionalAccountRequestCommand, Result>
{
    private const string NotFoundError = "Demande introuvable.";
    private const string NotSuspendedError = "Seul un compte suspendu peut être réactivé.";
    private const string NotEligibleError = "Les documents requis pour ce rôle ne sont pas tous approuvés.";

    private readonly IProfessionalAccountRequestRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly DocumentEligibilityChecker _eligibilityChecker;

    public ReactivateProfessionalAccountRequestCommandHandler(
        IProfessionalAccountRequestRepository repository, IUserRepository userRepository,
        DocumentEligibilityChecker eligibilityChecker)
    {
        _repository = repository;
        _userRepository = userRepository;
        _eligibilityChecker = eligibilityChecker;
    }

    public async Task<Result> Handle(ReactivateProfessionalAccountRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != ProfessionalAccountStatus.Suspended)
        {
            return Result.Failure(NotSuspendedError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var eligibility = await _eligibilityChecker.CheckAsync(request.UserId, request.Role, utcNow, cancellationToken);

        if (!eligibility.IsEligible)
        {
            return Result.Failure(NotEligibleError, ErrorType.Validation);
        }

        var reactivated = await _repository.TryReactivateAsync(
            request.Id, command.ReviewerId, utcNow, command.ReviewComment, cancellationToken);

        if (!reactivated)
        {
            return Result.Failure(NotSuspendedError, ErrorType.Conflict);
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
