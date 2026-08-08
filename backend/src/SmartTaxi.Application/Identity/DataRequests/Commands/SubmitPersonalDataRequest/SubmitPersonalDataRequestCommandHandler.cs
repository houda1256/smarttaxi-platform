using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Domain.Identity.DataRequests.Entities;

namespace SmartTaxi.Application.Identity.DataRequests.Commands.SubmitPersonalDataRequest;

public sealed class SubmitPersonalDataRequestCommandHandler
    : ICommandHandler<SubmitPersonalDataRequestCommand, Result<Guid>>
{
    private const string AlreadyPendingError = "Une demande de ce type est déjà en attente de traitement.";

    private readonly IPersonalDataRequestRepository _repository;

    public SubmitPersonalDataRequestCommandHandler(IPersonalDataRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(SubmitPersonalDataRequestCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetPendingForUserAndTypeAsync(command.UserId, command.RequestType, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(AlreadyPendingError, ErrorType.Conflict);
        }

        var request = new PersonalDataRequest(command.UserId, command.RequestType, DateTime.UtcNow);
        await _repository.AddAsync(request, cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}
