namespace SmartTaxi.API.Contracts.Identity.Documents;

public sealed record RejectDocumentRequest(string RejectionReason, string? ReviewComment);
