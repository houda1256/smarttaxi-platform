using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.CloseSupportIncident;

public sealed record CloseSupportIncidentCommand(Guid IncidentId) : ICommand<Result>;
