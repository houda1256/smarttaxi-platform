using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Identity.Professional.Commands.RejectProfessionalAccountRequest;

public sealed class RejectProfessionalAccountRequestCommandHandler
    : ICommandHandler<RejectProfessionalAccountRequestCommand, Result>
{
    private const string NotFoundError = "Demande introuvable.";
    private const string NotPendingError = "Cette demande n'est plus en attente de revue.";
    private const string MissingReasonError = "Un motif de rejet est requis.";

    private readonly IProfessionalAccountRequestRepository _repository;

    public RejectProfessionalAccountRequestCommandHandler(IProfessionalAccountRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectProfessionalAccountRequestCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Result.Failure(MissingReasonError, ErrorType.Validation);
        }

        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != ProfessionalAccountStatus.PendingReview)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var rejected = await _repository.TryRejectAsync(
            request.Id, command.ReviewerId, DateTime.UtcNow, command.RejectionReason, command.ReviewComment, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
