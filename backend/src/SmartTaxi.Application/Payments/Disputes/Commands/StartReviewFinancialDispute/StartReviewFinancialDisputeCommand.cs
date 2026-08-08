using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Disputes.Commands.StartReviewFinancialDispute;

public sealed record StartReviewFinancialDisputeCommand(Guid AssignedFinanceManagerId, Guid DisputeId) : ICommand<Result>;
