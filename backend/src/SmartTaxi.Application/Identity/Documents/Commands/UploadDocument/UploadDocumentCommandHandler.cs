using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;

namespace SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;

public sealed class UploadDocumentCommandHandler : ICommandHandler<UploadDocumentCommand, Result<UploadDocumentResult>>
{
    private const string DuplicateError = "Un document identique a déjà été soumis pour ce type de document.";

    private readonly IUserDocumentRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly DocumentUploadValidator _validator;

    public UploadDocumentCommandHandler(
        IUserDocumentRepository repository, IFileStorageService fileStorage, DocumentUploadValidator validator)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<Result<UploadDocumentResult>> Handle(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAndReadAsync(command.Content, command.DeclaredMimeType, cancellationToken);

        if (!validation.IsSuccess)
        {
            return Result<UploadDocumentResult>.Failure(validation.Error!, validation.ErrorType!.Value);
        }

        var content = validation.Value!;

        var isDuplicate = await _repository.ExistsWithHashAsync(
            command.UserId, command.DocumentType, content.Sha256, cancellationToken);

        if (isDuplicate)
        {
            return Result<UploadDocumentResult>.Failure(DuplicateError, ErrorType.Conflict);
        }

        var storageKey = DocumentFileNaming.CreateStorageKey(command.UserId, command.DeclaredMimeType);
        await using var writeStream = new MemoryStream(content.Bytes);
        await _fileStorage.SaveAsync(storageKey, writeStream, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var document = UserDocument.Upload(
            command.UserId,
            command.DocumentType,
            storageKey,
            DocumentFileNaming.SanitizeDisplayFileName(command.OriginalFileName),
            command.DeclaredMimeType,
            content.Bytes.LongLength,
            content.Sha256,
            command.IssueDate,
            command.ExpirationDate,
            utcNow);

        await _repository.AddAsync(document, cancellationToken);

        return Result<UploadDocumentResult>.Success(new UploadDocumentResult(document.Id, document.Status));
    }
}
