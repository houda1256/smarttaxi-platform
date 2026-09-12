using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Application.Identity.Professional;

public sealed record ProfessionalAccountRequestSummary(
    Guid Id,
    Guid UserId,
    UserRole Role,
    ProfessionalAccountStatus Status,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string? RejectionReason,
    string? ReviewComment,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProfessionalAccountRequestSummary FromEntity(ProfessionalAccountRequest request) => new(
        request.Id,
        request.UserId,
        request.Role,
        request.Status,
        request.ReviewedBy,
        request.ReviewedAt,
        request.RejectionReason,
        request.ReviewComment,
        request.CreatedAt,
        request.UpdatedAt);
}
