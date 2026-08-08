namespace SmartTaxi.API.Contracts.Identity.Documents;

public sealed record ReplaceDocumentResponse(Guid NewDocumentId, int NewVersion);
