using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Contracts;

namespace SmartTaxi.Application.Support.Queries.GetSupportTicketDetails;

public sealed record GetSupportTicketDetailsQuery(Guid TicketId, Guid RequestingUserId) : IQuery<Result<SupportTicketDetails>>;
