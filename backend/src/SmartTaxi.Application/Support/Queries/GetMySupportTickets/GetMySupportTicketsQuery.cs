using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetMySupportTickets;

public sealed record GetMySupportTicketsQuery(Guid RequesterUserId, int PageNumber, int PageSize) : IQuery<PagedResult<SupportTicket>>;
