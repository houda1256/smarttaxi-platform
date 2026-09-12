using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class ProfessionalAccountRequestRepository : IProfessionalAccountRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ProfessionalAccountRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProfessionalAccountRequest request, CancellationToken cancellationToken)
    {
        await _context.ProfessionalAccountRequests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<ProfessionalAccountRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken) =>
        _context.ProfessionalAccountRequests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetForUserAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await _context.ProfessionalAccountRequests.Where(request => request.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProfessionalAccountRequest>> GetPendingAsync(CancellationToken cancellationToken) =>
        await _context.ProfessionalAccountRequests
            .Where(request => request.Status == ProfessionalAccountStatus.PendingReview)
            .ToListAsync(cancellationToken);

    public Task<ProfessionalAccountRequest?> GetActiveForUserAndRoleAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken) =>
        _context.ProfessionalAccountRequests.FirstOrDefaultAsync(
            request => request.UserId == userId && request.Role == role
                && (request.Status == ProfessionalAccountStatus.PendingReview || request.Status == ProfessionalAccountStatus.Approved),
            cancellationToken);

    public async Task<bool> TryApproveAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.ProfessionalAccountRequests
            .Where(request => request.Id == requestId && request.Status == ProfessionalAccountStatus.PendingReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, ProfessionalAccountStatus.Approved)
                .SetProperty(request => request.ReviewedBy, reviewedBy)
                .SetProperty(request => request.ReviewedAt, utcNow)
                .SetProperty(request => request.ReviewComment, reviewComment)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRejectAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken)
    {
        var rows = await _context.ProfessionalAccountRequests
            .Where(request => request.Id == requestId && request.Status == ProfessionalAccountStatus.PendingReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, ProfessionalAccountStatus.Rejected)
                .SetProperty(request => request.ReviewedBy, reviewedBy)
                .SetProperty(request => request.ReviewedAt, utcNow)
                .SetProperty(request => request.RejectionReason, rejectionReason)
                .SetProperty(request => request.ReviewComment, reviewComment)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySuspendAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.ProfessionalAccountRequests
            .Where(request => request.Id == requestId && request.Status == ProfessionalAccountStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, ProfessionalAccountStatus.Suspended)
                .SetProperty(request => request.ReviewedBy, reviewedBy)
                .SetProperty(request => request.ReviewedAt, utcNow)
                .SetProperty(request => request.ReviewComment, reviewComment)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryReactivateAsync(
        Guid requestId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.ProfessionalAccountRequests
            .Where(request => request.Id == requestId && request.Status == ProfessionalAccountStatus.Suspended)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, ProfessionalAccountStatus.Approved)
                .SetProperty(request => request.ReviewedBy, reviewedBy)
                .SetProperty(request => request.ReviewedAt, utcNow)
                .SetProperty(request => request.ReviewComment, reviewComment)
                .SetProperty(request => request.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
