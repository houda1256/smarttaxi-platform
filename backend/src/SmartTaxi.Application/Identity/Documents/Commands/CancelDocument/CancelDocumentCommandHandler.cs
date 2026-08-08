using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.CancelDocument;

/// <summary>
/// The API exposes this as DELETE for REST semantics, but it never destroys
/// the row — a Pending document transitions to Cancelled, preserving its
/// metadata, hash, and audit trail. Reviewed documents cannot be cancelled.
/// </summary>
public sealed class CancelDocumentCommandHandler : ICommandHandler<CancelDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotPendingError = "Seul un document en attente de revue peut être annulé.";

    private readonly IUserDocumentRepository _repository;

    public CancelDocumentCommandHandler(IUserDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CancelDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(command.DocumentId, cancellationToken);

        // Ownership mismatch and "doesn't exist" return the identical outcome
        // — never confirm another user's document id.
        if (document is null || document.UserId != command.UserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (document.Status != DocumentStatus.Pending)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var cancelled = await _repository.TryCancelAsync(document.Id, command.UserId, utcNow, cancellationToken);

        if (!cancelled)
        {
            // Lost a race against a reviewer action (or another cancel) that
            // landed between the read above and this atomic write.
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
