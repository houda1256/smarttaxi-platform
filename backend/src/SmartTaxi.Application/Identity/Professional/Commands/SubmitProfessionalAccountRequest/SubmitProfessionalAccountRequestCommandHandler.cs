using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Professional;
using SmartTaxi.Domain.Identity.Professional.Entities;

namespace SmartTaxi.Application.Identity.Professional.Commands.SubmitProfessionalAccountRequest;

public sealed class SubmitProfessionalAccountRequestCommandHandler
    : ICommandHandler<SubmitProfessionalAccountRequestCommand, Result<Guid>>
{
    private const string UnsupportedRoleError = "Ce rôle n'est pas éligible à une demande de compte professionnel.";
    private const string UserNotFoundError = "Utilisateur introuvable.";
    private const string AlreadyHasActiveRequestError =
        "Une demande est déjà en attente ou approuvée pour ce rôle.";

    private readonly IUserRepository _userRepository;
    private readonly IProfessionalAccountRequestRepository _repository;

    public SubmitProfessionalAccountRequestCommandHandler(
        IUserRepository userRepository, IProfessionalAccountRequestRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(SubmitProfessionalAccountRequestCommand command, CancellationToken cancellationToken)
    {
        if (!ProfessionalRoles.IsProfessionalRole(command.Role))
        {
            return Result<Guid>.Failure(UnsupportedRoleError, ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<Guid>.Failure(UserNotFoundError, ErrorType.NotFound);
        }

        var existing = await _repository.GetActiveForUserAndRoleAsync(command.UserId, command.Role, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(AlreadyHasActiveRequestError, ErrorType.Conflict);
        }

        var request = new ProfessionalAccountRequest(command.UserId, command.Role, DateTime.UtcNow);
        await _repository.AddAsync(request, cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}
