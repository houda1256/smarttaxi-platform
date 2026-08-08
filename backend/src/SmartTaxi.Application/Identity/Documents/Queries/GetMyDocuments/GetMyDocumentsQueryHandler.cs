using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetMyDocuments;

public sealed class GetMyDocumentsQueryHandler
    : IQueryHandler<GetMyDocumentsQuery, IReadOnlyCollection<UserDocumentSummary>>
{
    private readonly IUserDocumentRepository _repository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetMyDocumentsQueryHandler(IUserDocumentRepository repository, IDocumentExpirationPolicy expirationPolicy)
    {
        _repository = repository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<IReadOnlyCollection<UserDocumentSummary>> Handle(
        GetMyDocumentsQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // Read-repair: opportunistically settle any Approved-but-past-expiration
        // rows before listing, rather than requiring a background scheduler.
        await _repository.ExpireDueDocumentsAsync(utcNow, cancellationToken);

        var documents = await _repository.GetForUserAsync(query.UserId, cancellationToken);

        return documents
            .Select(document => UserDocumentSummary.FromEntity(document, utcNow, _expirationPolicy.ReminderLeadDays))
            .ToList();
    }
}
