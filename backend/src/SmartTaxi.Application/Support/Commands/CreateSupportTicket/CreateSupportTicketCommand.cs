using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateSupportTicket;

public sealed record CreateSupportTicketCommand(
    Guid RequesterUserId, SupportTicketCategory Category, string Subject, string Description, SupportTicketPriority Priority,
    SupportRelatedEntityType? RelatedEntityType, Guid? RelatedEntityId) : ICommand<Result<Guid>>;
