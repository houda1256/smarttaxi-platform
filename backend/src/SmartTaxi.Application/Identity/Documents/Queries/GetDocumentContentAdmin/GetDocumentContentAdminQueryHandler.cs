using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Queries.GetDocumentContentAdmin;

public sealed class GetDocumentContentAdminQueryHandler
    : IQueryHandler<GetDocumentContentAdminQuery, Result<DocumentContentResult>>
{
    private const string NotFoundError = "Document introuvable.";

    private readonly IUserDocumentRepository _repository;
    private readonly IDocumentAccessAuditRepository _auditRepository;
    private readonly IFileStorageService _fileStorage;

    public GetDocumentContentAdminQueryHandler(
        IUserDocumentRepository repository, IDocumentAccessAuditRepository auditRepository, IFileStorageService fileStorage)
    {
        _repository = repository;
        _auditRepository = auditRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<DocumentContentResult>> Handle(GetDocumentContentAdminQuery query, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentContentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        await _auditRepository.AddAsync(
            new DocumentAccessAuditEntry(
                document.Id, query.RequestedByUserId, DocumentAccessType.ContentDownload, DateTime.UtcNow, query.IpAddress),
            cancellationToken);

        var stream = await _fileStorage.OpenReadAsync(document.FileReference, cancellationToken);

        return Result<DocumentContentResult>.Success(new DocumentContentResult(stream, document.MimeType, document.FileName));
    }
}
