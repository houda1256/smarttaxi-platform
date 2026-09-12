using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.MarkIncidentFalsePositive;

public sealed record MarkIncidentFalsePositiveCommand(Guid IncidentId) : ICommand<Result>;
