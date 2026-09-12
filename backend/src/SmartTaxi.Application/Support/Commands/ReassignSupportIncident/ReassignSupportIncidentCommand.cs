using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ReassignSupportIncident;

public sealed record ReassignSupportIncidentCommand(Guid IncidentId, Guid NewAdminUserId) : ICommand<Result>;
