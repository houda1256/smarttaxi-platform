using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.SuspendDocument;

/// <summary>Suspends a currently Approved document — e.g., discovered fraudulent or revoked after the fact.</summary>
public sealed class SuspendDocumentCommandHandler : ICommandHandler<SuspendDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotApprovedError = "Seul un document approuvé peut être suspendu.";
    private const string SelfReviewError = "Vous ne pouvez pas examiner votre propre document.";

    private readonly IUserDocumentRepository _repository;

    public SuspendDocumentCommandHandler(IUserDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendDocumentCommand command, CancellationToken cancellationToken)
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

        if (document.Status != DocumentStatus.Approved)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var suspended = await _repository.TrySuspendAsync(
            document.Id, command.ReviewerId, DateTime.UtcNow, command.ReviewComment, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
