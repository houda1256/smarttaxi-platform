using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of SupportTicketRepository's atomic conditional updates — same "reflection SetProperty helper" convention as every other Fake*Repository, since SupportTicket deliberately exposes no public status-mutation method.</summary>
public sealed class FakeSupportTicketRepository : ISupportTicketRepository
{
    private readonly Dictionary<Guid, SupportTicket> _tickets = new();

    public Task<bool> TryAddAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        _tickets[ticket.Id] = ticket;
        return Task.FromResult(true);
    }

    public Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken) =>
        Task.FromResult(_tickets.GetValueOrDefault(ticketId));

    public Task<PagedResult<SupportTicket>> GetForRequesterAsync(Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _tickets.Values.Where(t => t.RequesterUserId == requesterUserId).ToList();
        return Task.FromResult(new PagedResult<SupportTicket>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<SupportTicket>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _tickets.Values.ToList();
        return Task.FromResult(new PagedResult<SupportTicket>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TryAssignAsync(Guid ticketId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.Status != SupportTicketStatus.Open || ticket.AssignedAdminUserId is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(ticket, nameof(SupportTicket.Status), SupportTicketStatus.Assigned);
        SetProperty(ticket, nameof(SupportTicket.AssignedAdminUserId), adminUserId);
        SetProperty(ticket, nameof(SupportTicket.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReassignAsync(Guid ticketId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.Status == SupportTicketStatus.Closed)
        {
            return Task.FromResult(false);
        }

        SetProperty(ticket, nameof(SupportTicket.AssignedAdminUserId), newAdminUserId);
        SetProperty(ticket, nameof(SupportTicket.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryTransitionAsync(
        Guid ticketId, IReadOnlyCollection<SupportTicketStatus> allowedFromStatuses, SupportTicketStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || !allowedFromStatuses.Contains(ticket.Status)
            || (requiredAdminUserId is not null && ticket.AssignedAdminUserId != requiredAdminUserId))
        {
            return Task.FromResult(false);
        }

        SetProperty(ticket, nameof(SupportTicket.Status), newStatus);
        SetProperty(ticket, nameof(SupportTicket.UpdatedAtUtc), utcNow);

        switch (newStatus)
        {
            case SupportTicketStatus.Resolved:
                SetProperty(ticket, nameof(SupportTicket.Resolution), resolution);
                SetProperty(ticket, nameof(SupportTicket.ResolvedAtUtc), utcNow);
                break;
            case SupportTicketStatus.Closed:
                SetProperty(ticket, nameof(SupportTicket.ClosedAtUtc), utcNow);
                break;
            case SupportTicketStatus.Reopened:
                SetProperty(ticket, nameof(SupportTicket.ReopenedAtUtc), utcNow);
                break;
        }

        return Task.FromResult(true);
    }

    public Task<bool> TryAddRequesterMessageAndAdvanceAsync(
        Guid ticketId, Guid requesterUserId, string body, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.RequesterUserId != requesterUserId
            || ticket.Status == SupportTicketStatus.Closed)
        {
            return Task.FromResult(false);
        }

        Messages.Add(SupportTicketMessage.Create(ticketId, requesterUserId, body, isInternalNote: false, utcNow));

        if (ticket.Status == SupportTicketStatus.WaitingForCustomer)
        {
            SetProperty(ticket, nameof(SupportTicket.Status), SupportTicketStatus.InProgress);
            SetProperty(ticket, nameof(SupportTicket.UpdatedAtUtc), utcNow);
        }

        return Task.FromResult(true);
    }

    public Task<bool> TrySetEscalatedIncidentIdAsync(Guid ticketId, Guid incidentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.EscalatedIncidentId is not null)
        {
            return Task.FromResult(false);
        }

        SetProperty(ticket, nameof(SupportTicket.EscalatedIncidentId), incidentId);
        SetProperty(ticket, nameof(SupportTicket.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    /// <summary>Test-only visibility into messages inserted via TryAddRequesterMessageAndAdvanceAsync — mirrors FakeFleetExpenseRepository's GetAllForTest() convention.</summary>
    public List<SupportTicketMessage> Messages { get; } = [];

    private static void SetProperty(SupportTicket ticket, string propertyName, object? value) =>
        typeof(SupportTicket).GetProperty(propertyName)!.SetValue(ticket, value);
}
