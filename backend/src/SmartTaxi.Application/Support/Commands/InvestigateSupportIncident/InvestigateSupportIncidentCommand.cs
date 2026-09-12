using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.InvestigateSupportIncident;

public sealed record InvestigateSupportIncidentCommand(Guid IncidentId, Guid AdminUserId) : ICommand<Result>;
