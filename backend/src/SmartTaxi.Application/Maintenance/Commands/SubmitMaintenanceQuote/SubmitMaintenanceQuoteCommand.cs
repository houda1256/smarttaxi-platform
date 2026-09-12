using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.SubmitMaintenanceQuote;

public sealed record SubmitMaintenanceQuoteCommand(Guid RequestId, Guid GarageUserId, decimal EstimatedCost) : ICommand<Result>;
