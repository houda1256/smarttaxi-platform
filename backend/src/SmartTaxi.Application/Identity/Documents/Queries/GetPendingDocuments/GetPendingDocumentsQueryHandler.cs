using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetPendingDocuments;

public sealed class GetPendingDocumentsQueryHandler
    : IQueryHandler<GetPendingDocumentsQuery, IReadOnlyCollection<UserDocumentSummary>>
{
    private readonly IUserDocumentRepository _repository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetPendingDocumentsQueryHandler(IUserDocumentRepository repository, IDocumentExpirationPolicy expirationPolicy)
    {
        _repository = repository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<IReadOnlyCollection<UserDocumentSummary>> Handle(
        GetPendingDocumentsQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var documents = await _repository.GetPendingAsync(cancellationToken);

        return documents
            .Select(document => UserDocumentSummary.FromEntity(document, utcNow, _expirationPolicy.ReminderLeadDays))
            .ToList();
    }
}
