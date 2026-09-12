using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputesForReview;

public sealed record GetFinancialDisputesForReviewQuery(
    FinancialDisputeStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<FinancialDispute>>;
