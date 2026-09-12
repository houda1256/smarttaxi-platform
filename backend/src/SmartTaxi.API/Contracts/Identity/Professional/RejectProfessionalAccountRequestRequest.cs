namespace SmartTaxi.API.Contracts.Identity.Professional;

public sealed record RejectProfessionalAccountRequestRequest(string RejectionReason, string? ReviewComment);
