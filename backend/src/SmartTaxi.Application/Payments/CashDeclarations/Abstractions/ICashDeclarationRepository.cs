using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Application.Payments.CashDeclarations.Abstractions;

public interface ICashDeclarationRepository
{
    Task AddAsync(CashDeclaration declaration, CancellationToken cancellationToken);

    Task<CashDeclaration?> GetByIdAsync(Guid declarationId, CancellationToken cancellationToken);

    Task<PagedResult<CashDeclaration>> GetForDriverAsync(
        Guid driverId, CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<CashDeclaration>> GetForReviewAsync(
        CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> TryStartReviewAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryDisputeAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySettleAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken);
}
