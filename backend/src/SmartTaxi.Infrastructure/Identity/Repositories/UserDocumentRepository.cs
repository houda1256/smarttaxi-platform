using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class UserDocumentRepository : IUserDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public UserDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserDocument document, CancellationToken cancellationToken)
    {
        await _context.UserDocuments.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<UserDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken) =>
        _context.UserDocuments.FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);

    public async Task<IReadOnlyCollection<UserDocument>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.UserDocuments.Where(document => document.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<UserDocument>> GetPendingAsync(CancellationToken cancellationToken) =>
        await _context.UserDocuments
            .Where(document => document.Status == DocumentStatus.Pending)
            .ToListAsync(cancellationToken);

    public Task<UserDocument?> GetLatestForUserAndTypeAsync(
        Guid userId, DocumentType documentType, CancellationToken cancellationToken) =>
        _context.UserDocuments
            .Where(document => document.UserId == userId && document.DocumentType == documentType
                && document.Status != DocumentStatus.Replaced)
            .OrderByDescending(document => document.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsWithHashAsync(
        Guid userId, DocumentType documentType, string sha256, CancellationToken cancellationToken) =>
        _context.UserDocuments.AnyAsync(
            document => document.UserId == userId && document.DocumentType == documentType && document.Sha256 == sha256
                && document.Status != DocumentStatus.Replaced && document.Status != DocumentStatus.Cancelled,
            cancellationToken);

    public async Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.UserDocuments
            .Where(document => document.Id == documentId && document.Status == DocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Approved)
                .SetProperty(document => document.ReviewedBy, reviewedBy)
                .SetProperty(document => document.ReviewedAt, utcNow)
                .SetProperty(document => document.ReviewComment, reviewComment)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRejectAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken)
    {
        var rows = await _context.UserDocuments
            .Where(document => document.Id == documentId && document.Status == DocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Rejected)
                .SetProperty(document => document.ReviewedBy, reviewedBy)
                .SetProperty(document => document.ReviewedAt, utcNow)
                .SetProperty(document => document.RejectionReason, rejectionReason)
                .SetProperty(document => document.ReviewComment, reviewComment)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySuspendAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.UserDocuments
            .Where(document => document.Id == documentId && document.Status == DocumentStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Suspended)
                .SetProperty(document => document.ReviewedBy, reviewedBy)
                .SetProperty(document => document.ReviewedAt, utcNow)
                .SetProperty(document => document.ReviewComment, reviewComment)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryCancelAsync(
        Guid documentId, Guid ownerUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.UserDocuments
            .Where(document => document.Id == documentId && document.UserId == ownerUserId
                && document.Status == DocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Cancelled)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        _context.UserDocuments
            .Where(document => document.Status == DocumentStatus.Approved
                && document.ExpirationDate != null && document.ExpirationDate <= utcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Expired)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

    public async Task<bool> TryReplaceAsync(
        UserDocument current, UserDocument replacement, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.UserDocuments
            .Where(document => document.Id == current.Id && document.Status != DocumentStatus.Replaced)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, DocumentStatus.Replaced)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        if (rows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _context.UserDocuments.AddAsync(replacement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
