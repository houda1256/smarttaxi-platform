using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Disputes.Commands.EscalateFinancialDispute;

public sealed record EscalateFinancialDisputeCommand(Guid EscalatedBy, Guid DisputeId) : ICommand<Result>;
