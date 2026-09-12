using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;

public sealed record UploadDocumentResult(Guid DocumentId, DocumentStatus Status);
