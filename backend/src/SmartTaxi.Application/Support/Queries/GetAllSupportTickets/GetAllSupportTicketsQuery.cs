using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetAllSupportTickets;

public sealed record GetAllSupportTicketsQuery(int PageNumber, int PageSize) : IQuery<PagedResult<SupportTicket>>;
