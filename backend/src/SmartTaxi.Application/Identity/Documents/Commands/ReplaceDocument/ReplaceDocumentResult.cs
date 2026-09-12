namespace SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;

public sealed record ReplaceDocumentResult(Guid NewDocumentId, int NewVersion);
