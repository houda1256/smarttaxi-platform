using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;

public sealed record UploadDocumentCommand(
    Guid UserId,
    DocumentType DocumentType,
    Stream Content,
    string OriginalFileName,
    string DeclaredMimeType,
    DateTime? IssueDate,
    DateTime? ExpirationDate) : ICommand<Result<UploadDocumentResult>>;
