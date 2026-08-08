using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.ApproveDocument;

public sealed class ApproveDocumentCommandHandler : ICommandHandler<ApproveDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotPendingError = "Ce document n'est plus en attente de revue.";
    private const string SelfReviewError = "Vous ne pouvez pas examiner votre propre document.";

    private readonly IUserDocumentRepository _repository;

    public ApproveDocumentCommandHandler(IUserDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveDocumentCommand command, CancellationToken cancellationToken)
    {
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

        var approved = await _repository.TryApproveAsync(
            document.Id, command.ReviewerId, DateTime.UtcNow, command.ReviewComment, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
