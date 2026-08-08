using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class VehicleDocumentRepository : IVehicleDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
    {
        await _context.VehicleDocuments.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<VehicleDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken) =>
        _context.VehicleDocuments.FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);

    public async Task<IReadOnlyCollection<VehicleDocument>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await _context.VehicleDocuments.Where(document => document.VehicleId == vehicleId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VehicleDocument>> GetPendingAsync(CancellationToken cancellationToken) =>
        await _context.VehicleDocuments
            .Where(document => document.Status == VehicleDocumentStatus.Pending)
            .ToListAsync(cancellationToken);

    public Task<VehicleDocument?> GetLatestForVehicleAndTypeAsync(
        Guid vehicleId, VehicleDocumentType documentType, CancellationToken cancellationToken) =>
        _context.VehicleDocuments
            .Where(document => document.VehicleId == vehicleId && document.DocumentType == documentType
                && document.Status != VehicleDocumentStatus.Replaced)
            .OrderByDescending(document => document.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsWithHashAsync(
        Guid vehicleId, VehicleDocumentType documentType, string sha256, CancellationToken cancellationToken) =>
        _context.VehicleDocuments.AnyAsync(
            document => document.VehicleId == vehicleId && document.DocumentType == documentType && document.Sha256 == sha256
                && document.Status != VehicleDocumentStatus.Replaced, cancellationToken);

    public async Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        var rows = await _context.VehicleDocuments
            .Where(document => document.Id == documentId && document.Status == VehicleDocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, VehicleDocumentStatus.Approved)
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
        var rows = await _context.VehicleDocuments
            .Where(document => document.Id == documentId && document.Status == VehicleDocumentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, VehicleDocumentStatus.Rejected)
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
        var rows = await _context.VehicleDocuments
            .Where(document => document.Id == documentId && document.Status == VehicleDocumentStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, VehicleDocumentStatus.Suspended)
                .SetProperty(document => document.ReviewedBy, reviewedBy)
                .SetProperty(document => document.ReviewedAt, utcNow)
                .SetProperty(document => document.ReviewComment, reviewComment)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        _context.VehicleDocuments
            .Where(document => document.Status == VehicleDocumentStatus.Approved
                && document.ExpirationDate != null && document.ExpirationDate <= utcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, VehicleDocumentStatus.Expired)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

    public async Task<bool> TryReplaceAsync(
        VehicleDocument current, VehicleDocument replacement, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.VehicleDocuments
            .Where(document => document.Id == current.Id && document.Status != VehicleDocumentStatus.Replaced)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(document => document.Status, VehicleDocumentStatus.Replaced)
                .SetProperty(document => document.UpdatedAt, utcNow), cancellationToken);

        if (rows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await _context.VehicleDocuments.AddAsync(replacement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
