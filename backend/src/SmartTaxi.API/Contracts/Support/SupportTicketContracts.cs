using SmartTaxi.Application.Support.Contracts;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.API.Contracts.Support;

public sealed record CreateSupportTicketRequest(
    SupportTicketCategory Category, string Subject, string Description, SupportTicketPriority Priority,
    SupportRelatedEntityType? RelatedEntityType, Guid? RelatedEntityId);

public sealed record AddTicketMessageRequest(string Body);

public sealed record ResolveSupportTicketRequest(string Resolution);

public sealed record ReassignSupportTicketRequest(Guid NewAdminUserId);

public sealed record EscalateTicketToIncidentRequest(SupportIncidentType IncidentType, SupportIncidentSeverity Severity, string? TitleOverride);

public sealed record SupportTicketMessageResponse(Guid Id, Guid AuthorUserId, string Body, bool IsInternalNote, DateTime CreatedAtUtc)
{
    public static SupportTicketMessageResponse FromEntity(SupportTicketMessage message) =>
        new(message.Id, message.AuthorUserId, message.Body, message.IsInternalNote, message.CreatedAtUtc);
}

public sealed record SupportTicketResponse(
    Guid Id, string TicketNumber, Guid RequesterUserId, string Category, string Subject, string Description, string Priority,
    string? RelatedEntityType, Guid? RelatedEntityId, string Status, Guid? AssignedAdminUserId, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, DateTime? ResolvedAtUtc, DateTime? ClosedAtUtc, DateTime? ReopenedAtUtc, string? Resolution,
    Guid? EscalatedIncidentId)
{
    public static SupportTicketResponse FromEntity(SupportTicket ticket) => new(
        ticket.Id, ticket.TicketNumber, ticket.RequesterUserId, ticket.Category.ToString(), ticket.Subject, ticket.Description,
        ticket.Priority.ToString(), ticket.RelatedEntityType?.ToString(), ticket.RelatedEntityId, ticket.Status.ToString(),
        ticket.AssignedAdminUserId, ticket.CreatedAtUtc, ticket.UpdatedAtUtc, ticket.ResolvedAtUtc, ticket.ClosedAtUtc,
        ticket.ReopenedAtUtc, ticket.Resolution, ticket.EscalatedIncidentId);
}

public sealed record SupportTicketDetailsResponse(SupportTicketResponse Ticket, IReadOnlyCollection<SupportTicketMessageResponse> Messages)
{
    public static SupportTicketDetailsResponse FromDto(SupportTicketDetails details) => new(
        SupportTicketResponse.FromEntity(details.Ticket), details.Messages.Select(SupportTicketMessageResponse.FromEntity).ToList());
}
