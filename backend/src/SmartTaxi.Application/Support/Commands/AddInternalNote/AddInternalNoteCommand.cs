using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.AddInternalNote;

public sealed record AddInternalNoteCommand(Guid TicketId, Guid AdminUserId, string Body) : ICommand<Result>;
