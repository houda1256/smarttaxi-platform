using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ResolveSupportIncident;

public sealed record ResolveSupportIncidentCommand(Guid IncidentId, Guid AdminUserId, string Resolution) : ICommand<Result>;
