using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetDocumentByIdAdmin;

public sealed class GetDocumentByIdAdminQueryHandler
    : IQueryHandler<GetDocumentByIdAdminQuery, Result<UserDocumentSummary>>
{
    private const string NotFoundError = "Document introuvable.";

    private readonly IUserDocumentRepository _repository;
    private readonly IDocumentAccessAuditRepository _auditRepository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GetDocumentByIdAdminQueryHandler(
        IUserDocumentRepository repository, IDocumentAccessAuditRepository auditRepository, IDocumentExpirationPolicy expirationPolicy)
    {
        _repository = repository;
        _auditRepository = auditRepository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<Result<UserDocumentSummary>> Handle(GetDocumentByIdAdminQuery query, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var document = await _repository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<UserDocumentSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        // Admin/reviewer reading someone else's document metadata is a
        // sensitive access — audited even though it's just a metadata read.
        if (document.UserId != query.RequestedByUserId)
        {
            await _auditRepository.AddAsync(
                new DocumentAccessAuditEntry(
                    document.Id, query.RequestedByUserId, DocumentAccessType.MetadataRead, utcNow, query.IpAddress),
                cancellationToken);
        }

        return Result<UserDocumentSummary>.Success(
            UserDocumentSummary.FromEntity(document, utcNow, _expirationPolicy.ReminderLeadDays));
    }
}
