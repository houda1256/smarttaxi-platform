using System.Security.Cryptography;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Domain.Identity.DataRequests.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.DataRequests.Commands.ProcessPersonalDataRequest;

/// <summary>
/// Deletion/anonymization never issue a physical DELETE against Users — the
/// row is preserved (UserDocuments and audit entries reference it), only its
/// personally-identifiable fields are scrubbed via User.Anonymize.
/// </summary>
public sealed class ProcessPersonalDataRequestCommandHandler : ICommandHandler<ProcessPersonalDataRequestCommand, Result>
{
    private const string NotFoundError = "Demande introuvable.";
    private const string NotPendingError = "Cette demande a déjà été traitée.";
    private const string UserNotFoundError = "Utilisateur introuvable.";

    private readonly IPersonalDataRequestRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly PersonalDataExportBuilder _exportBuilder;

    public ProcessPersonalDataRequestCommandHandler(
        IPersonalDataRequestRepository repository, IUserRepository userRepository, PersonalDataExportBuilder exportBuilder)
    {
        _repository = repository;
        _userRepository = userRepository;
        _exportBuilder = exportBuilder;
    }

    public async Task<Result> Handle(ProcessPersonalDataRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != PersonalDataRequestStatus.Pending)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;

        if (!command.Approve)
        {
            var rejected = await _repository.TryRejectAsync(
                request.Id, command.ProcessedBy, utcNow, command.ProcessingNotes, cancellationToken);

            return rejected ? Result.Success() : Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserNotFoundError, ErrorType.NotFound);
        }

        string? resultReference = null;

        switch (request.RequestType)
        {
            case PersonalDataRequestType.Export:
                resultReference = await _exportBuilder.BuildAndStoreAsync(user.Id, cancellationToken);
                break;

            case PersonalDataRequestType.Deletion:
                AnonymizeUser(user);
                user.Deactivate();
                await _userRepository.UpdateAsync(user, cancellationToken);
                break;

            case PersonalDataRequestType.Anonymization:
                AnonymizeUser(user);
                await _userRepository.UpdateAsync(user, cancellationToken);
                break;
        }

        var completed = await _repository.TryCompleteAsync(
            request.Id, command.ProcessedBy, utcNow, command.ProcessingNotes, resultReference, cancellationToken);

        return completed ? Result.Success() : Result.Failure(NotPendingError, ErrorType.Conflict);
    }

    private static void AnonymizeUser(Domain.Identity.Entities.User user)
    {
        var anonymizedEmail = Email.Create($"deleted-{Guid.NewGuid()}@anonymized.smarttaxi.invalid");
        var unusableHash = HashedPassword.Create(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        user.Anonymize(anonymizedEmail, unusableHash);
    }
}
