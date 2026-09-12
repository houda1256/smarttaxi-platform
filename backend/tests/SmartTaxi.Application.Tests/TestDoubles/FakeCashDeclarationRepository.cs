using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeCashDeclarationRepository : ICashDeclarationRepository
{
    private readonly Dictionary<Guid, CashDeclaration> _declarationsById = new();

    public Task AddAsync(CashDeclaration declaration, CancellationToken cancellationToken)
    {
        _declarationsById[declaration.Id] = declaration;
        return Task.CompletedTask;
    }

    public Task<CashDeclaration?> GetByIdAsync(Guid declarationId, CancellationToken cancellationToken) =>
        Task.FromResult(_declarationsById.GetValueOrDefault(declarationId));

    public Task<PagedResult<CashDeclaration>> GetForDriverAsync(
        Guid driverId, CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _declarationsById.Values.Where(d => d.DriverId == driverId);

        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<CashDeclaration>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<CashDeclaration>> GetForReviewAsync(
        CashDeclarationStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _declarationsById.Values.AsEnumerable();

        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<CashDeclaration>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TryStartReviewAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(declarationId, CashDeclarationStatus.Submitted, CashDeclarationStatus.UnderReview, null, utcNow);

    public Task<bool> TryApproveAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_declarationsById.TryGetValue(declarationId, out var declaration)
            || declaration.Status is not (CashDeclarationStatus.Submitted or CashDeclarationStatus.UnderReview))
        {
            return Task.FromResult(false);
        }

        SetProperty(declaration, nameof(CashDeclaration.Status), CashDeclarationStatus.Approved);
        SetProperty(declaration, nameof(CashDeclaration.ReviewedBy), reviewedBy);
        SetProperty(declaration, nameof(CashDeclaration.ReviewedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryDisputeAsync(Guid declarationId, Guid reviewedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_declarationsById.TryGetValue(declarationId, out var declaration)
            || declaration.Status is not (CashDeclarationStatus.Submitted or CashDeclarationStatus.UnderReview))
        {
            return Task.FromResult(false);
        }

        SetProperty(declaration, nameof(CashDeclaration.Status), CashDeclarationStatus.Disputed);
        SetProperty(declaration, nameof(CashDeclaration.ReviewedBy), reviewedBy);
        SetProperty(declaration, nameof(CashDeclaration.ReviewedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySettleAsync(Guid declarationId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_declarationsById.TryGetValue(declarationId, out var declaration)
            || declaration.Status is not (CashDeclarationStatus.Approved or CashDeclarationStatus.Disputed))
        {
            return Task.FromResult(false);
        }

        SetProperty(declaration, nameof(CashDeclaration.Status), CashDeclarationStatus.Settled);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransition(Guid declarationId, CashDeclarationStatus from, CashDeclarationStatus to, Guid? reviewedBy, DateTime utcNow)
    {
        if (!_declarationsById.TryGetValue(declarationId, out var declaration) || declaration.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(declaration, nameof(CashDeclaration.Status), to);
        return Task.FromResult(true);
    }

    private static void SetProperty(CashDeclaration declaration, string propertyName, object? value) =>
        typeof(CashDeclaration).GetProperty(propertyName)!.SetValue(declaration, value);
}
