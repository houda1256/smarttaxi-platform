using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Enums;

namespace SmartTaxi.Domain.Identity.Professional.Entities;

/// <summary>
/// Status transitions are enforced as atomic conditional SQL updates in the
/// repository (same pattern as 2c/2d), not as mutating methods here — the
/// guard condition has one source of truth.
/// </summary>
public sealed class ProfessionalAccountRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public UserRole Role { get; private set; }
    public ProfessionalAccountStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ReviewComment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProfessionalAccountRequest()
    {
    }

    public ProfessionalAccountRequest(Guid userId, UserRole role, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Role = role;
        Status = ProfessionalAccountStatus.PendingReview;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
