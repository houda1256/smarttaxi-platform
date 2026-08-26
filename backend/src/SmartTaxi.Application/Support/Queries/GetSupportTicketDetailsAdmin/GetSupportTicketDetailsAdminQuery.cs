using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Contracts;

namespace SmartTaxi.Application.Support.Queries.GetSupportTicketDetailsAdmin;

public sealed record GetSupportTicketDetailsAdminQuery(Guid TicketId) : IQuery<Result<SupportTicketDetails>>;
