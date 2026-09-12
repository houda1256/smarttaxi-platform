using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;

public sealed record ReplaceDocumentCommand(
    Guid UserId,
    Guid DocumentId,
    Stream Content,
    string OriginalFileName,
    string DeclaredMimeType,
    DateTime? IssueDate,
    DateTime? ExpirationDate) : ICommand<Result<ReplaceDocumentResult>>;
