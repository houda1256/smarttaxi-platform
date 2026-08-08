using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeCashRegisterSessionRepository : ICashRegisterSessionRepository
{
    private readonly Dictionary<Guid, CashRegisterSession> _sessionsById = new();
    private readonly Dictionary<Guid, Guid> _ownerBySessionId = new();

    public Task<bool> TryAddAsync(CashRegisterSession session, CancellationToken cancellationToken)
    {
        if (_sessionsById.Values.Any(s => s.CashRegisterId == session.CashRegisterId && s.Status == CashRegisterSessionStatus.Open))
        {
            return Task.FromResult(false);
        }

        _sessionsById[session.Id] = session;
        return Task.FromResult(true);
    }

    public Task<CashRegisterSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(_sessionsById.GetValueOrDefault(sessionId));

    public Task<CashRegisterSession?> GetOpenForRegisterAsync(Guid cashRegisterId, CancellationToken cancellationToken)
    {
        var session = _sessionsById.Values.FirstOrDefault(s => s.CashRegisterId == cashRegisterId && s.Status == CashRegisterSessionStatus.Open);
        return Task.FromResult(session);
    }

    public Task<PagedResult<CashRegisterSession>> GetForOwnerAsync(
        Guid ownerId, CashRegisterSessionStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _sessionsById.Values.Where(s => _ownerBySessionId.GetValueOrDefault(s.Id) == ownerId);

        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<CashRegisterSession>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TryCloseAsync(
        Guid sessionId, decimal closingExpectedBalance, decimal closingActualBalance, decimal difference,
        string? differenceReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_sessionsById.TryGetValue(sessionId, out var session) || session.Status != CashRegisterSessionStatus.Open)
        {
            return Task.FromResult(false);
        }

        SetProperty(session, nameof(CashRegisterSession.Status), CashRegisterSessionStatus.Closed);
        SetProperty(session, nameof(CashRegisterSession.ClosingExpectedBalance), closingExpectedBalance);
        SetProperty(session, nameof(CashRegisterSession.ClosingActualBalance), closingActualBalance);
        SetProperty(session, nameof(CashRegisterSession.Difference), difference);
        SetProperty(session, nameof(CashRegisterSession.DifferenceReason), differenceReason);
        SetProperty(session, nameof(CashRegisterSession.ClosedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReconcileAsync(Guid sessionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_sessionsById.TryGetValue(sessionId, out var session) || session.Status != CashRegisterSessionStatus.Closed)
        {
            return Task.FromResult(false);
        }

        SetProperty(session, nameof(CashRegisterSession.Status), CashRegisterSessionStatus.Reconciled);
        return Task.FromResult(true);
    }

    public Task<bool> TryDisputeAsync(Guid sessionId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_sessionsById.TryGetValue(sessionId, out var session) || session.Status != CashRegisterSessionStatus.Closed)
        {
            return Task.FromResult(false);
        }

        SetProperty(session, nameof(CashRegisterSession.Status), CashRegisterSessionStatus.Disputed);
        SetProperty(session, nameof(CashRegisterSession.DifferenceReason), reason);
        return Task.FromResult(true);
    }

    /// <summary>Test-only helper so GetForOwnerAsync can filter — the real Infrastructure implementation joins through CashRegisterBox.OwnerId instead of needing this.</summary>
    public void AssociateWithOwner(Guid sessionId, Guid ownerId) => _ownerBySessionId[sessionId] = ownerId;

    private static void SetProperty(CashRegisterSession session, string propertyName, object? value) =>
        typeof(CashRegisterSession).GetProperty(propertyName)!.SetValue(session, value);
}
