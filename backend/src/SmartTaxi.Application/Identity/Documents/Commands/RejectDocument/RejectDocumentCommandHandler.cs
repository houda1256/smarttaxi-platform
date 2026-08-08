using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.RejectDocument;

public sealed class RejectDocumentCommandHandler : ICommandHandler<RejectDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotPendingError = "Ce document n'est plus en attente de revue.";
    private const string SelfReviewError = "Vous ne pouvez pas examiner votre propre document.";
    private const string MissingReasonError = "Un motif de rejet est requis.";

    private readonly IUserDocumentRepository _repository;

    public RejectDocumentCommandHandler(IUserDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectDocumentCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Result.Failure(MissingReasonError, ErrorType.Validation);
        }

        var document = await _repository.GetByIdAsync(command.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (document.UserId == command.ReviewerId)
        {
            return Result.Failure(SelfReviewError, ErrorType.Forbidden);
        }

        if (document.Status != DocumentStatus.Pending)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var rejected = await _repository.TryRejectAsync(
            document.Id, command.ReviewerId, DateTime.UtcNow, command.RejectionReason, command.ReviewComment, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
