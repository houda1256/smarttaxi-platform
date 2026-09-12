using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Queries.GetAdminPayments;

public sealed record GetAdminPaymentsQuery(
    Guid? CustomerId, Guid? DriverId, Guid? OwnerId, PaymentStatus? Status, DateTime? FromDate, DateTime? ToDate,
    int PageNumber, int PageSize) : IQuery<PagedResult<PaymentSummary>>;
