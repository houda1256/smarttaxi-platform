using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Identity.Professional.Commands.SuspendProfessionalAccountRequest;

/// <summary>
/// Suspending a professional account also revokes the granted role so
/// permissions are actually cut off, not just flagged. If the role removal
/// would violate "a user must always keep at least one role" (only possible
/// if this professional role is somehow the user's only role, not expected
/// since registration always grants a base role first), the role is left in
/// place but the request itself is still marked Suspended.
/// </summary>
public sealed class SuspendProfessionalAccountRequestCommandHandler
    : ICommandHandler<SuspendProfessionalAccountRequestCommand, Result>
{
    private const string NotFoundError = "Demande introuvable.";
    private const string NotApprovedError = "Seul un compte professionnel approuvé peut être suspendu.";

    private readonly IProfessionalAccountRequestRepository _repository;
    private readonly IUserRepository _userRepository;

    public SuspendProfessionalAccountRequestCommandHandler(
        IProfessionalAccountRequestRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(SuspendProfessionalAccountRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != ProfessionalAccountStatus.Approved)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var suspended = await _repository.TrySuspendAsync(
            request.Id, command.ReviewerId, utcNow, command.ReviewComment, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is not null && user.CanRemoveRole(request.Role))
        {
            user.RemoveRole(request.Role);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return Result.Success();
    }
}
