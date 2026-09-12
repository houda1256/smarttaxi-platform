using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;

public sealed class ReplaceDocumentCommandHandler : ICommandHandler<ReplaceDocumentCommand, Result<ReplaceDocumentResult>>
{
    private const string NotFoundError = "Document introuvable.";
    private const string AlreadyReplacedError = "Ce document a déjà été remplacé par une version plus récente.";
    private const string DuplicateError = "Un document identique a déjà été soumis pour ce type de document.";
    private const string ConcurrentReplaceError = "Ce document vient d'être remplacé par une autre requête.";

    private readonly IUserDocumentRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly DocumentUploadValidator _validator;

    public ReplaceDocumentCommandHandler(
        IUserDocumentRepository repository, IFileStorageService fileStorage, DocumentUploadValidator validator)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<Result<ReplaceDocumentResult>> Handle(ReplaceDocumentCommand command, CancellationToken cancellationToken)
    {
        var current = await _repository.GetByIdAsync(command.DocumentId, cancellationToken);

        // Ownership mismatch and "doesn't exist" return the identical outcome
        // — never confirm another user's document id.
        if (current is null || current.UserId != command.UserId)
        {
            return Result<ReplaceDocumentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (current.Status == DocumentStatus.Replaced)
        {
            return Result<ReplaceDocumentResult>.Failure(AlreadyReplacedError, ErrorType.Conflict);
        }

        var validation = await _validator.ValidateAndReadAsync(command.Content, command.DeclaredMimeType, cancellationToken);

        if (!validation.IsSuccess)
        {
            return Result<ReplaceDocumentResult>.Failure(validation.Error!, validation.ErrorType!.Value);
        }

        var content = validation.Value!;

        var isDuplicate = await _repository.ExistsWithHashAsync(
            command.UserId, current.DocumentType, content.Sha256, cancellationToken);

        if (isDuplicate)
        {
            return Result<ReplaceDocumentResult>.Failure(DuplicateError, ErrorType.Conflict);
        }

        var storageKey = DocumentFileNaming.CreateStorageKey(command.UserId, command.DeclaredMimeType);
        await using var writeStream = new MemoryStream(content.Bytes);
        await _fileStorage.SaveAsync(storageKey, writeStream, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var replacement = current.CreateReplacement(
            storageKey,
            DocumentFileNaming.SanitizeDisplayFileName(command.OriginalFileName),
            command.DeclaredMimeType,
            content.Bytes.LongLength,
            content.Sha256,
            command.IssueDate,
            command.ExpirationDate,
            utcNow);

        var replaced = await _repository.TryReplaceAsync(current, replacement, utcNow, cancellationToken);

        if (!replaced)
        {
            return Result<ReplaceDocumentResult>.Failure(ConcurrentReplaceError, ErrorType.Conflict);
        }

        return Result<ReplaceDocumentResult>.Success(new ReplaceDocumentResult(replacement.Id, replacement.Version));
    }
}
