using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetMyFinancialDisputes;

public sealed record GetMyFinancialDisputesQuery(
    Guid RaisedBy, FinancialDisputeStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<FinancialDispute>>;
