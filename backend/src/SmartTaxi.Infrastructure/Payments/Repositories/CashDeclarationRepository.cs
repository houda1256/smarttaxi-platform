using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class CashDeclarationRepository : ICashDeclarationRepository
{
    private static readonly CashDeclarationStatus[] ApprovableFromStatuses =
        [CashDeclarationStatus.Submitted, CashDeclarationStatus.UnderReview];

    private static readonly CashDeclarationStatus[] SettleableFromStatuses =
        [CashDeclarationStatus.Approved, CashDeclarationStatus.Disputed];

    private readonly ApplicationDbContext _context;

    public CashDeclarationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CashDeclaration declaration, CancellationToken cancellationToken)
    {
        await _context.CashDeclarations.AddAsync(declaration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<CashDeclaration?> GetByIdAsync(Guid declarationId, CancellationToken cancellationToken) =>
        _context.CashDeclarations.FirstOrDefaultAsync(declaration => declaration.Id == declarationId, cancellationToken);

    public async Task<PagedResult<CashDeclaration>> GetForDriverAsync(
        Guid driverId, CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.CashDeclarations.Where(declaration => declaration.DriverId == driverId);

        if (status is not null)
        {
            query = query.Where(declaration => declaration.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(declaration => declaration.SubmittedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CashDeclaration>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<CashDeclaration>> GetForReviewAsync(
        CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.CashDeclarations.AsQueryable();

        if (status is not null)
        {
            query = query.Where(declaration => declaration.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(declaration => declaration.SubmittedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CashDeclaration>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TryStartReviewAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashDeclarations
            .Where(declaration => declaration.Id == declarationId && declaration.Status == CashDeclarationStatus.Submitted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(declaration => declaration.Status, CashDeclarationStatus.UnderReview), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryApproveAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashDeclarations
            .Where(declaration => declaration.Id == declarationId && ApprovableFromStatuses.Contains(declaration.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(declaration => declaration.Status, CashDeclarationStatus.Approved)
                .SetProperty(declaration => declaration.ReviewedBy, reviewedBy)
                .SetProperty(declaration => declaration.ReviewedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryDisputeAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashDeclarations
            .Where(declaration => declaration.Id == declarationId && ApprovableFromStatuses.Contains(declaration.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(declaration => declaration.Status, CashDeclarationStatus.Disputed)
                .SetProperty(declaration => declaration.ReviewedBy, reviewedBy)
                .SetProperty(declaration => declaration.ReviewedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySettleAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.CashDeclarations
            .Where(declaration => declaration.Id == declarationId && SettleableFromStatuses.Contains(declaration.Status))
            .ExecuteUpdateAsync(setters => setters.SetProperty(declaration => declaration.Status, CashDeclarationStatus.Settled), cancellationToken);

        return rows == 1;
    }
}
