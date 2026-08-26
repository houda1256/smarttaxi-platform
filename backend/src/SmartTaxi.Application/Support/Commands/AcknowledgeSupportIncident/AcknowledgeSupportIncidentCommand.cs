using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.AcknowledgeSupportIncident;

public sealed record AcknowledgeSupportIncidentCommand(Guid IncidentId, Guid AdminUserId) : ICommand<Result>;
