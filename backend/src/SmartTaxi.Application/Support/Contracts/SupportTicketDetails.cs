using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Contracts;

/// <summary>Composite read model — Messages is already filtered appropriately by the query handler (requester-visible-only vs admin-full) before this record is constructed, never filtered downstream.</summary>
public sealed record SupportTicketDetails(SupportTicket Ticket, IReadOnlyCollection<SupportTicketMessage> Messages);
