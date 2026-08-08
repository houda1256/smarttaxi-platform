using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputeById;

public sealed record GetFinancialDisputeByIdQuery(Guid DisputeId) : IQuery<Result<FinancialDispute>>;
