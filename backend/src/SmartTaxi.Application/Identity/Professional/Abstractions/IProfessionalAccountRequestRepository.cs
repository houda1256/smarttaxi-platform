using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;

namespace SmartTaxi.Application.Identity.Professional.Abstractions;

public interface IProfessionalAccountRequestRepository
{
    Task AddAsync(ProfessionalAccountRequest request, CancellationToken cancellationToken);

    Task<ProfessionalAccountRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>Used to block a duplicate submission while one is already pending or already approved.</summary>
    Task<ProfessionalAccountRequest?> GetActiveForUserAndRoleAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    Task<bool> TryReactivateAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);
}
