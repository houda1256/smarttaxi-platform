using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ReopenSupportIncident;

public sealed record ReopenSupportIncidentCommand(Guid IncidentId) : ICommand<Result>;
