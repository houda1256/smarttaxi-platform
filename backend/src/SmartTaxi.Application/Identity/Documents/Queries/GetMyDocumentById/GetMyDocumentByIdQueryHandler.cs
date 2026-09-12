using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentById;

public sealed class GetMyDocumentByIdQueryHandler
    : IQueryHandler<GetMyDocumentByIdQuery, Result<UserDocumentSummary>>
{
    private const string NotFoundError = "Document introuvable.";

    private readonly IUserDocumentRepository _repository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetMyDocumentByIdQueryHandler(IUserDocumentRepository repository, IDocumentExpirationPolicy expirationPolicy)
    {
        _repository = repository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<Result<UserDocumentSummary>> Handle(GetMyDocumentByIdQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        await _repository.ExpireDueDocumentsAsync(utcNow, cancellationToken);

        var document = await _repository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null || document.UserId != query.UserId)
        {
            return Result<UserDocumentSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<UserDocumentSummary>.Success(
            UserDocumentSummary.FromEntity(document, utcNow, _expirationPolicy.ReminderLeadDays));
    }
}
