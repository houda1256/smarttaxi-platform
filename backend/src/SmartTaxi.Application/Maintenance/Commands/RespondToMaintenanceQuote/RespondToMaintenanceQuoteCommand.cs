using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceQuote;

public sealed record RespondToMaintenanceQuoteCommand(Guid RequestId, Guid OwnerUserId, bool IsAccepted) : ICommand<Result>;
