using SmartTaxi.Application.Identity.Professional;

namespace SmartTaxi.API.Contracts.Identity.Professional;

public sealed record ProfessionalAccountRequestResponse(
    Guid Id,
    Guid UserId,
    string Role,
    string Status,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string? RejectionReason,
    string? ReviewComment,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProfessionalAccountRequestResponse FromSummary(ProfessionalAccountRequestSummary summary) => new(
        summary.Id,
        summary.UserId,
        summary.Role.ToString(),
        summary.Status.ToString(),
        summary.ReviewedBy,
        summary.ReviewedAt,
        summary.RejectionReason,
        summary.ReviewComment,
        summary.CreatedAt,
        summary.UpdatedAt);
}
